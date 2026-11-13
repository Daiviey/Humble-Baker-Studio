# Automation painting

The C# port includes a full automation painter: draw a shape on the canvas, translate it into
one or more automation lanes, and keep the painting itself as an exact visual record.

Open **Tools -> Automation paint**.

## Quick start

1. Choose a **Target**: the selected track, a patch-graph FX/group/bus node, or a shared bus.
2. Choose the **FX / module** and **Parameter**.
3. Paint a curve: left = start of the song, right = end, bottom = low value, top = high value.
4. Press **Write to Lane** (or **Apply to Piano Roll**).
5. Press **Play** to hear it. The status bar confirms how many points were written.

Selecting a parameter draws its existing lane back onto the canvas. If a lane has a saved
painted snapshot, the exact painting is restored instead of an approximation.

## Canvas and brushes

- Seven brushes: **Hard, Soft, Smooth, Streak, Gradient, Smudge, Smear**.
- **Size** is in screen pixels, so the brush stays the same visual size at every canvas
  resolution.
- The brush uses elliptical cell radii internally, keeping it round even when the canvas is
  stretched by the control.
- **Clear Canvas** removes the painting. **Clear Lane** removes only the selected parameter's
  lane. **Reset Track Auto** removes every lane on the target.

## Resolution

The canvas resolution is selectable:

| Option | Columns x rows | Typical use |
|---|---|---|
| Normal | 512 x 128 | Fast sketching |
| High | 1024 x 256 | Default; detailed curves |
| Extreme | 2048 x 512 | Dense modulation and fills |
| Ultra | 4096 x 512 | Maximum detail |

Changing resolution resamples the current painting (nearest-neighbour when increasing,
bilinear when decreasing). For maximum detail, choose the resolution before painting.

Playback cost does not grow with canvas resolution:

- `AutoLane.At` uses binary search, so a lane with tens of thousands of points still takes
  `O(log n)` per lookup.
- Automation is evaluated at the renderer's audio block rate, not per canvas pixel.
- The canvas is drawn at screen resolution by sampling the grid, so a 4096-column canvas does
  not make repainting proportionally slower.

## Targets and wired parameters

Track automation supports:

- `vol`, `pan`, `lp`, `hp`
- any numeric patch parameter via `p:Name` (for example `p:Cutoff`, `p:FilterEnv`,
  `p:FmAmount`, `p:Unison`)

Track FX and graph/bus FX automation supports the parameters the renderer currently processes:

- `f:Cutoff`, `f:Resonance`
- `f:Drive`, `f:Fold`, `f:Bits`
- `f:RingMix`, `f:RingFreq`
- `f:DelayMix`
- `f:ReverbMix`
- `f:Chorus`
- `f:TapeStop`
- `f:Width`
- `f:CompThresh`

Other parameters can still be painted and stored, but the status bar warns that the renderer
does not process them yet. If you hear nothing, check that warning first. Synth patch (`p:`)
parameters apply to track targets only.

## Mapping and detail

- **Mapping**
  - `Exact shape` - one point per painted column, no smoothing (default)
  - `Smooth curve` - three-point smoothing
  - `Quantize steps` - snaps values to a step grid
  - `Linear ramp` - blends toward a straight line between the first and last value
- **Z axis**
  - `Depth (scale value)` - brightness scales the value toward the target's neutral setting
  - `Direct value` - brightness sets the value directly
  - `Off (position only)` - only the vertical position matters
- **Shape detail**
  - `Curve (single value)` - the column centreline
  - `Stroke (follow rows)` - preserves thick vertical strokes by emitting both edges as fast
    modulation

## Layers and intersections

A single automation parameter is one-dimensional: there is only one value at a given time.
The layer system uses the rest of the canvas to carry multiple splines.

- Paint two or more separate waves.
- **Keep crossing strokes separate** is on by default. Each mouse stroke is captured into its
  own buffer, so waves that intersect still remain separate layers. Connected-region extraction
  is used only when there are no captured strokes (for example imported paint).
- Press **Extract layers**. Each stroke or region becomes a layer:
  - `Layer 1`, `Layer 2`, ... (top to bottom)
  - the layer's peak painted brightness is its amplitude
- Select a layer in the list and assign its parameter with **Layer parameter**.
- **Normalize by amplitude** gives faint and bright waves comparable weight. **Gain** scales
  the whole layer.
- **Layer mix** controls what happens when several layers target the same parameter:
  - `Add` - sum the deviations from neutral
  - `Multiply` - multiply the normalized deviations
  - `Max` - keep the strongest deviation
  - `Replace` - the last layer wins
- **Write layers** creates one lane per parameter. Layers with the same parameter are combined
  into a single lane.

Layer outlines are drawn in colour over the canvas. Turn off **Show layer outlines** to hide
them.

## Preserved painting snapshots

Each written lane stores a display-only snapshot of the painted canvas:

- one byte per cell (256 brightness levels), compressed with Deflate
- at the exact canvas resolution it was painted at
- restored by the editor when you return to the parameter; the resolution selector updates to
  match
- ignored by the renderer and by playback

This keeps the visual result nearly identical to the painting without turning the automation
lane into a large per-pixel data structure. The lane itself remains a normal 1D curve.

The snapshot is saved in the project file as `preview` / `previewCols` / `previewRows` on the
automation lane. The PowerShell studio ignores these extra fields; saving the project from the
PowerShell studio will drop the snapshots. Automation points, lane parameters and playback are
unaffected.

## Project format

Automation lane points are arrays:

```
[beat, value, brightness, verticalLow, verticalHigh]
```

The first two values are the automation curve. The extra values preserve the painted stroke
when a lane is redrawn without a snapshot. Older readers use only the first two values.

## Troubleshooting

- **No audible change** - choose a wired `f:` or `p:` parameter, or a track/mixer target. The
  status bar names parameters the renderer does not process yet.
- **Painting looks blocky after changing resolution** - the painting was made at a lower
  resolution before being resampled. Choose the target resolution before painting.
- **Layer list is empty** - paint at least one stroke and press **Extract layers**. If strokes
  were disabled, connected regions are used.
- **Crossing waves merged into one layer** - make sure **Keep crossing strokes separate** is
  checked and paint each wave as its own mouse stroke.
- **Visuals changed after opening in the PowerShell studio** - the PowerShell app does not
  store the display snapshot and will drop it when it saves the project.
