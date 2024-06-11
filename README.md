# Unity-RefractiveFlowRender

Render refractive objects in Unity HDRP (High Definition Rendering Pipeline), generate corresponding semantic masks, and refractive flow.

## Usage

### Step 1: Generate RGB images, calibrations, normals and masks

1. Open the `HDRPRefraction` Unity project.
2. Select the `controller` GameObject in the Inspector window.
3. Check the required options (e.g., training set or validation set, number of images).
4. Click the `Run` button to generate images and records.

### Step 2: Generate refractive flow

An example to generate refractive flows:
   ```shell
   python generate_refractive_flow.py --main_path "./HDRPRefraction/train_cg" --num_imgs 5000
   ```
Note: If you exclusively generate calibrations for refractive flow generation, then after this step you can delete the Calibration folder.

### Step 3: Generate active depth

An example to generate active depth:
   ```shell
   python generate_active_depth.py --main_path "./HDRPRefraction/train_cg" --num_imgs 5000
   ```
