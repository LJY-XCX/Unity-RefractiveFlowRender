# Unity-RefractiveFlowRender

Render refractive objects in Unity HDRP (High Definition Rendering Pipeline), generate corresponding semantic masks, and refractive flow.

## Usage

### Step 1: Generate refractive RGB images

1. Open the `HDRPRefraction` Unity project.
2. Navigate to the `RGB` scene.
3. Select the `controller` GameObject in the Inspector window.
4. Check the required options (e.g., training set or validation set, number of images).
5. Click the `Run` button to generate images and records.

### Step 2: Generate calibration images

1. In the same Unity project, open the `Calibration` scene.
2. Configure options in the `controller` GameObject.
3. Click `Run` to generate graycode images for refractive flow calibration.

### Step 3: Generate binary mask and semantic segmentation mask

1. Open the `RefractiveMask` Unity project and the `Mask` scene.
2. Configure options in the `controller` GameObject.
3. Click `Run` to generate binary and semantic masks.

### Step 4: Generate refractive flow

1. Use Python to generate refractive flows:
   ```shell
   # Generate for training set
   python generate_refractive_flow.py train

   # Generate for validation set
   python generate_refractive_flow.py valid
