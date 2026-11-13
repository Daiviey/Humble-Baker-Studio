using System.IO.Pipes;
using System.Text;
using HumbleBakerStudio.VstBridge;

string? pipeName = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--pipe" && i + 1 < args.Length) pipeName = args[++i];
}

if (string.IsNullOrWhiteSpace(pipeName))
{
    Console.Error.WriteLine("usage: hbs-vst-bridge --pipe <name>");
    return 2;
}

using var host = new BridgeHost();
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };

while (!stop.IsCancellationRequested)
{
    using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1,
        PipeTransmissionMode.Byte, PipeOptions.None);
    try
    {
        pipe.WaitForConnection();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("accept failed: " + ex.Message);
        break;
    }

    using var reader = new BinaryReader(pipe, Encoding.UTF8, leaveOpen: true);
    using var writer = new BinaryWriter(pipe, Encoding.UTF8, leaveOpen: true);
    bool shutdown = false;
    try
    {
        while (pipe.IsConnected && !shutdown)
        {
            int op;
            try { op = reader.ReadInt32(); }
            catch (EndOfStreamException) { break; }
            catch (IOException) { break; }

            switch (op)
            {
                case Protocol.OpPing:
                    Protocol.WriteOk(writer);
                    Protocol.WriteString(writer, "0.1.0");
                    writer.Write(44100);
                    writer.Flush();
                    break;

                case Protocol.OpScan:
                {
                    var plugins = host.Scan();
                    Protocol.WriteOk(writer);
                    writer.Write(plugins.Count);
                    foreach (var plugin in plugins)
                    {
                        Protocol.WriteString(writer, plugin.Name);
                        Protocol.WriteString(writer, plugin.Vendor);
                        Protocol.WriteString(writer, plugin.Path);
                        Protocol.WriteString(writer, plugin.ClassId);
                        writer.Write(plugin.IsInstrument);
                    }
                    writer.Flush();
                    break;
                }

                case Protocol.OpEffect:
                {
                    string path = Protocol.ReadString(reader);
                    string classId = Protocol.ReadString(reader);
                    int sampleRate = reader.ReadInt32();
                    int stateLength = reader.ReadInt32();
                    byte[] state = reader.ReadBytes(stateLength);
                    bool wantState = reader.ReadBoolean();
                    float[] left = Protocol.ReadFloats(reader);
                    float[] right = Protocol.ReadFloats(reader);
                    try
                    {
                        var result = host.ProcessEffect(path, classId, sampleRate, state, wantState, left, right, out byte[]? newState);
                        Protocol.WriteOk(writer);
                        Protocol.WriteFloats(writer, result[0]);
                        Protocol.WriteFloats(writer, result[1]);
                        writer.Write(newState != null);
                        if (newState != null) { writer.Write(newState.Length); writer.Write(newState); }
                        writer.Flush();
                    }
                    catch (Exception ex)
                    {
                        Protocol.WriteError(writer, ex.Message);
                    }
                    break;
                }

                case Protocol.OpInstrument:
                {
                    string path = Protocol.ReadString(reader);
                    string classId = Protocol.ReadString(reader);
                    int sampleRate = reader.ReadInt32();
                    int stateLength = reader.ReadInt32();
                    byte[] state = reader.ReadBytes(stateLength);
                    bool wantState = reader.ReadBoolean();
                    int frames = reader.ReadInt32();
                    double secondsPerBeat = reader.ReadDouble();
                    bool loop = reader.ReadBoolean();
                    int noteCount = reader.ReadInt32();
                    var notes = new List<BridgeNote>(Math.Max(0, noteCount));
                    for (int i = 0; i < noteCount; i++)
                    {
                        notes.Add(new BridgeNote
                        {
                            StartBeat = reader.ReadDouble(),
                            LenBeats = reader.ReadDouble(),
                            Pitch = reader.ReadInt32(),
                            Vel = reader.ReadInt32()
                        });
                    }
                    try
                    {
                        var result = host.RenderInstrument(path, classId, sampleRate, state, wantState, notes, secondsPerBeat, frames, loop, out byte[]? newState);
                        Protocol.WriteOk(writer);
                        Protocol.WriteFloats(writer, result[0]);
                        Protocol.WriteFloats(writer, result[1]);
                        writer.Write(newState != null);
                        if (newState != null) { writer.Write(newState.Length); writer.Write(newState); }
                        writer.Flush();
                    }
                    catch (Exception ex)
                    {
                        Protocol.WriteError(writer, ex.Message);
                    }
                    break;
                }

                case Protocol.OpCapture:
                {
                    string path = Protocol.ReadString(reader);
                    string classId = Protocol.ReadString(reader);
                    int sampleRate = reader.ReadInt32();
                    try
                    {
                        byte[] state = host.Capture(path, classId, sampleRate);
                        Protocol.WriteOk(writer);
                        writer.Write(state.Length);
                        writer.Write(state);
                        writer.Flush();
                    }
                    catch (Exception ex)
                    {
                        Protocol.WriteError(writer, ex.Message);
                    }
                    break;
                }

                case Protocol.OpRelease:
                {
                    string path = Protocol.ReadString(reader);
                    string classId = Protocol.ReadString(reader);
                    host.Release(path, classId);
                    Protocol.WriteOk(writer);
                    break;
                }

                case Protocol.OpShutdown:
                    Protocol.WriteOk(writer);
                    shutdown = true;
                    stop.Cancel();
                    break;

                default:
                    Protocol.WriteError(writer, "Unknown op " + op);
                    break;
            }
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("session failed: " + ex);
    }

    // One studio session owns one bridge. When the pipe closes the studio has quit (or crashed),
    // so exit instead of waiting forever for a new client.
    if (!stop.IsCancellationRequested) break;
}

return 0;
