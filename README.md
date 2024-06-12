# Unity-RefractiveFlowRender

Render refractive objects in Unity HDRP (High Definition Rendering Pipeline), generating corresponding surface normals, semantic masks, depths, and refractive flow.

## Usage
### Step 1: Add your prefabs and other resources in Assets (Optional)
1. Put your prefabs under ./Assets/Prefabs.
2. Put your transparent materials under ./Assets/Recourses/GlassMaterials.
3. Put your skyboxes under ./Assets/Recourses/HDRIImages.
4. Put your opaque materials under ./Assets/Recourses/Materials.
5. Put your table materials under ./Assets/Recourses/TableMaterials.

Note: It's not necessary to put your resources in the folders above. Just remember to follow the second point in the step two.

### Step 2: Generate RGB images, calibrations, normals and masks

1. Open the `HDRPRefraction` Unity project and swich to the "Image" scene under ./New.
2. Select the `controller` GameObject in the Inspector window. Add your prefabs, transparent materials, skyboxes, opaque materials and table materials to the corresponding arrays.
3. Set the required options (e.g., training set or validation set, number of images).
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
