import os
import cv2
import glob
import numpy as np
import argparse
import matplotlib
import matplotlib.pyplot as plt
from tqdm import tqdm


def generate_numbers_from_graycode_imgs(imgs):
    binary_imgs = binarize_imgs(imgs)

    result = np.zeros(binary_imgs[-1].shape, dtype=np.int32)
    for idx in range(2, 20):  # here we just ignore the pure-black img and the pure-white img
        binary_img = binary_imgs[idx] > 127
        result += binary_img * (2 ** (19 - idx))

    result_x = result % 512
    result_y = result // 512

    return result_x, result_y


def get_images_graycode(base_path):
    image_paths = [os.path.join(base_path, f"graycode_{idx}.png") for idx in range(19)]

    imgs = [cv2.imread(image_path) for image_path in image_paths]
    black_code = np.zeros_like(imgs[0])

    imgs = [black_code] + imgs

    return generate_numbers_from_graycode_imgs(imgs)


def get_images(base_path):
    image_paths = [
        os.path.join(base_path, f'{idx}.png') for idx in range(1, 20)
    ]

    imgs = [cv2.imread(image_path) for image_path in image_paths]
    black_code = np.zeros_like(imgs[0])

    imgs = [black_code] + imgs

    return imgs


def trim_roi(imgs, roi):
    return [img[roi[0]:roi[1], roi[2]:roi[3]] for img in imgs]


def binarize_imgs(imgs):
    imgs = [cv2.cvtColor(img, cv2.COLOR_BGR2GRAY) for img in imgs]
    imgs = [cv2.threshold(img, 127, 255, cv2.THRESH_BINARY)[1] for img in imgs]
    return imgs


def get_flow(img_folder, roi=None, size=None):
    imgs = get_images(img_folder)
    if roi is not None:
        imgs = trim_roi(imgs, roi)
    if size is not None:
        imgs = [cv2.resize(img, size, interpolation=cv2.INTER_NEAREST) for img in imgs]

    return generate_numbers_from_graycode_imgs(imgs)


## Helper functions for flow
def flowToMap(F_mag, F_dir):
    sz = F_mag.shape
    flow_color = np.zeros((sz[0], sz[1], 3), dtype=float)
    flow_color[:, :, 0] = (F_dir + np.pi) / (2 * np.pi)
    # f_dir =(F_dir+np.pi) / (2 * np.pi)
    # flow_color[:,:,1] = np.clip(F_mag / (F_mag.shape[0]*0.5), 0, 1)
    flow_color[:, :, 1] = np.clip(F_mag / (512 / 2), 0, 1)
    flow_color[:, :, 2] = 1
    flow_color = matplotlib.colors.hsv_to_rgb(flow_color) * 255
    return flow_color


def flowToColor(flow):
    F_dx = flow[:, :, 1].copy().astype(float)
    F_dy = flow[:, :, 0].copy().astype(float)
    F_mag = np.sqrt(np.power(F_dx, 2) + np.power(F_dy, 2))
    F_dir = np.arctan2(F_dy, F_dx)
    flow_color = flowToMap(F_mag, F_dir)
    return flow_color.astype(np.uint8)


if __name__ == "__main__":
    reference_x, reference_y = get_images_graycode("./graycode_imgs")  # the "GT" gray code

    parser = argparse.ArgumentParser()
    parser.add_argument("--main_path", type=str, default="./HDRPRefraction/train", help="Path to main directory")
    parser.add_argument("--start_idx", type=int, default=0, help="Starting index of images")
    parser.add_argument("--num_imgs", type=int, default=5000, help="Number of images")
    parser.add_argument("--vis", action="store_true", help="Enable visualization")
    args = parser.parse_args()

    input_folder_name = os.path.join(args.main_path, 'calibration')
    output_folder_name = os.path.join(args.main_path, 'flow')
    os.makedirs(output_folder_name, exist_ok=True)
    # start the for-loop

    for idx in tqdm(range(args.start_idx, args.num_imgs)):
        real_flow_x, real_flow_y = get_flow(os.path.join(input_folder_name, str(idx)), roi=None)

        flow_x = real_flow_x - reference_x
        flow_y = real_flow_y - reference_y

        if args.vis:
            fig, ax = plt.subplots(1, 2)
            ax[0].imshow(flow_x)
            ax[1].imshow(flow_y)
            plt.tight_layout()
            plt.show()

        flow = np.concatenate([flow_x[..., None], flow_y[..., None]], axis=2)
        if args.vis:
            print(flow.shape, flow.dtype)
        # You can save the flow here to npy file
        np.save(os.path.join(output_folder_name, f'flow_{idx}.npy'), flow)

        if args.vis:
            # =============================================================================
            # The following code is just for visualization
            # flow = cv2.resize(flow, (512, 512), interpolation=cv2.INTER_NEAREST)

            flow_color = flowToColor(flow)
            # post-processing
            # flow_color = cv2.medianBlur(flow_color, 5)
            # flow_color = cv2.resize(flow_color, (512, 512))

            plt.imshow(flow_color)
            plt.show()

            # plt.imsave("flow_real_no_blur.png", flow_color)
            # np.save('flow_real_no_blur.npy', flow)
