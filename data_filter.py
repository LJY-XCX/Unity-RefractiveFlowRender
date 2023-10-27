import numpy as np
import cv2
import argparse
import os
from tqdm import tqdm


png_suffix = '_flow.png'
flo_suffix = '_flow.flo'


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument('mode')
    parser.add_argument('--background_dir', type=str, default='background')
    parser.add_argument('--calibration_dir', )

    args = parser.parse_args()
    return args


def filter1(flowpng, path=None): 
    # flow_gray = cv2.imread(flowpng,0)   
    flow_gray = cv2.cvtColor(flowpng,cv2.COLOR_BGR2GRAY)
    valid = np.sum((flow_gray>0)&(flow_gray<255)) 
    invalid = np.sum(flow_gray==0)
    valid_pc = valid/(valid+invalid)
    if valid_pc>=0.85:   #有效refractive flow比例大于0.95 如果生成速度太慢就改低一点
        '''save'''
        return True
    else:
        return False
        
         
def filter2(flowflo, path=None):  #flowflo shape:(512,512,2)
    # flowflo = load_flo(flowflo) 
    invalid = np.sum(flowflo>50)  #refractive flow任一数值不超过50
    if invalid==0:  
        '''save''' 
        return True
    else:
        return False


def load_flo(path):
    with open(path, mode='r') as flo:
        tag = np.fromfile(flo, np.float32, count=1)[0]
        width = np.fromfile(flo, np.int32, count=1)[0]
        height = np.fromfile(flo, np.int32, count=1)[0]
        nbands = 2 
        tmp = np.fromfile(flo, np.int16, count= nbands * width * height)
        flow = np.resize(tmp, ( int(height), int(width), int(nbands)))
    return flow


def filt_data(base_dir, config):
    counter = 0
    num_flows = len(os.listdir(base_dir)) // 2
    for idx in tqdm(range(num_flows)):
        flow_png = os.path.join(base_dir, str(idx) + png_suffix)
        flow_flo = os.path.join(base_dir, str(idx) + flo_suffix)
        # Check for png
        cv2_img = cv2.imread(flow_png)
        valid_png = filter1(cv2_img)
        # Check for flo
        flo = load_flo(flow_flo)
        valid_flo = filter2(flo)

        if valid_flo and valid_png:
            counter += 1
        
    print(counter)


if __name__ == '__main__':
    config = parse_args()
    base_dir = ''
    if config.mode == 'train':
        base_dir = './HDRPRefraction/train/refractive_flow/'
    elif config.mode == 'valid':
        base_dir = './HDRPRefraction/valid/refractive_flow/'
    else:
        print('Error mode!')
        exit(-1)
    
    filt_data(base_dir, config)
