import os
import cv2
import numpy as np
import pyrfuniverse.utils.active_depth_generate as dg
from tqdm import tqdm
import matplotlib.pyplot as plt
import argparse


left_extrinsic_matrix = np.array([[0., -1., 0., -0.0175],
                                  [0., 0., -1., 0.],
                                  [1., 0., 0., 0.],
                                  [0., 0., 0., 1.]])
right_extrinsic_matrix = np.array([[0., -1., 0., -0.072],
                                   [0., 0., -1., 0.],
                                   [1., 0., 0., 0.],
                                   [0., 0., 0., 1.]])
main_extrinsic_matrix = np.array([[0., -1., 0., 0.],
                                  [0., 0., -1., 0.],
                                  [1., 0., 0., 0.],
                                  [0., 0., 0., 1.]])

ir_intrinsic_matrix = np.array([[700, 0, 512],
                                [0, 700, 512],
                                [0, 0, 1]])

main_fov, main_size = 60, 1024
main_f = int(main_size / 2 / np.tan(main_fov / 2 / 180 * np.pi))
main_intrinsic_matrix = np.array([[main_f, 0, main_size/2],
                                  [0, main_f, main_size/2],
                                  [0, 0, 1]], dtype=np.int32)
# main_intrinsic_matrix = np.array([[600, 0, 512],
#                                   [0, 600, 512],
#                                   [0, 0, 1]])
# main_intrinsic_matrix = ir_intrinsic_matrix

parser = argparse.ArgumentParser()
parser.add_argument("--main_path", type=str, default="./HDRPRefraction/train_cg", help="Path to main directory")
parser.add_argument("--start_idx", type=int, default=0, help="Starting index of images")
parser.add_argument("--num_imgs", type=int, default=5000, help="Number of images")
parser.add_argument("--vis", action="store_true", help="Enable visualization")
args = parser.parse_args()

os.makedirs(os.path.join(args.main_path, 'active_depth'), exist_ok=True)

# for i in tqdm(range(num_imgs)):
for i in tqdm(range(args.start_idx, args.num_imgs)):
    ir_left_path = os.path.join(args.main_path, 'ir_left', f'ir_left_{i}.png')
    ir_right_path = os.path.join(args.main_path, 'ir_right', f'ir_right_{i}.png')

    ir_left = cv2.imread(ir_left_path, cv2.IMREAD_COLOR)[..., 2]
    ir_right = cv2.imread(ir_right_path, cv2.IMREAD_COLOR)[..., 2]

    active_depth = dg.calc_main_depth_from_left_right_ir(ir_left, ir_right,
                                                         left_extrinsic_matrix,
                                                         right_extrinsic_matrix,
                                                         main_extrinsic_matrix,
                                                         ir_intrinsic_matrix,
                                                         ir_intrinsic_matrix,
                                                         main_intrinsic_matrix,
                                                         lr_consistency=False,
                                                         main_cam_size=(
                                                             main_intrinsic_matrix[0, 2] * 2,
                                                             main_intrinsic_matrix[1, 2] * 2),
                                                         ndisp=128, use_census=True,
                                                         register_depth=True, census_wsize=7,
                                                         use_noise=False)

    active_depth = cv2.resize(active_depth, (512, 512), interpolation=cv2.INTER_NEAREST)

    png_active_depth = (active_depth * 1000).astype(np.uint16)  # TO Unit: mm
    active_depth_path = os.path.join(args.main_path, 'active_depth', f'active_depth_{i}.png')
    cv2.imwrite(active_depth_path, png_active_depth)

    if vis:
        f, axes = plt.subplots(2, 3)
        axes[0][0].imshow(active_depth)

        true_depth = cv2.imread(os.path.join(args.main_path, 'depth', f'depth_{i}.png'), -1)
        true_depth = true_depth * 3.0 / (2**16)
        axes[0][1].imshow(true_depth)

        delta = active_depth - true_depth
        axes[0][2].imshow(delta)

        # ir_left = cv2.imread(ir_left_path, cv2.IMREAD_COLOR)
        # axes[1][0].imshow(ir_left[..., 0])
        # axes[1][1].imshow(ir_left[..., 1])
        # axes[1][2].imshow(ir_left[..., 2])
        axes[1][0].imshow(ir_left)
        axes[1][1].imshow(ir_right)

        plt.tight_layout()
        plt.show()
