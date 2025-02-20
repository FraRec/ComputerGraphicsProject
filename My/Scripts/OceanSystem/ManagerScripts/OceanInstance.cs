using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanInstance {
    // Shader Local Groups
    const int LOCAL_WORK_GROUPS_X = 8;
    const int LOCAL_WORK_GROUPS_Y = 8;

    // Properties
    private int _N;
    private OceanManager oceanManager;

    // Textures
    public RenderTexture _h0;
    public RenderTexture _hkt_dy;
    public RenderTexture _hkt_dx;
    public RenderTexture _hkt_dz;
    public RenderTexture _butterfly;
    public RenderTexture _pingpong0;
    public RenderTexture _pingpong1;
    public RenderTexture _buffer_dy;
    public RenderTexture _buffer_dx;
    public RenderTexture _buffer_dz;
    public RenderTexture _displacements_dy;
    public RenderTexture _displacements_dx;
    public RenderTexture _displacements_dz;
    public RenderTexture _displacements;
    public RenderTexture _normals;
                         
    // Compute Shaders
    private ComputeShader _h0Shader;
    private ComputeShader _htShader;
    private ComputeShader _butterflyShader;
    private ComputeShader _fftShader;
    private ComputeShader _finalPassShader;
    private ComputeShader _normalMapShader;

    // Compute Shaders Kernels
    private int KERNEL_OCEAN_H0;
    private int KERNEL_OCEAN_HT;
    private int KERNEL_BUTTERFLYTEXTURE;
    private int KERNEL_HORIZONTAL_STEP_IFFT;
    private int KERNEL_VERTICAL_STEP_IFFT;
    private int KERNEL_OCEAN_FINALPASS;
    private int KERNEL_OCEAN_MERGEDISPLACEMENT;
    private int KERNEL_OCEAN_NORMALMAP;


    // Compute Buffers
    readonly ComputeBuffer paramsBuffer;

    public OceanInstance(int _Resolution, OceanManager oceanManager, ComputeShader h0Shader, ComputeShader htShader, ComputeShader butterflyShader, ComputeShader fftShader, ComputeShader finalPassShader, ComputeShader normalMapShader) {
        // VARIABLES
        this._N = _Resolution;
        this.oceanManager = oceanManager;

        // SHADERS
        this._h0Shader        = h0Shader;
        this._htShader        = htShader;
        this._butterflyShader = butterflyShader;
        this._fftShader       = fftShader;
        this._finalPassShader = finalPassShader;
        this._normalMapShader = normalMapShader;

        // DEFINE KERNELS
        //KERNEL_OCEAN_H0                = _h0Shader.FindKernel("Calculate_h0");
        KERNEL_OCEAN_HT                = _htShader.FindKernel("Calculate_ht");
        KERNEL_BUTTERFLYTEXTURE        = _butterflyShader.FindKernel("Calculate_ButterflyTexture");
        KERNEL_HORIZONTAL_STEP_IFFT    = _fftShader.FindKernel("HorizontalButterflies");
        KERNEL_VERTICAL_STEP_IFFT      = _fftShader.FindKernel("VerticalButterflies");
        KERNEL_OCEAN_FINALPASS         = _finalPassShader.FindKernel("Calculate_FinalPass");
        KERNEL_OCEAN_MERGEDISPLACEMENT = _finalPassShader.FindKernel("Merge_Displacements");
        KERNEL_OCEAN_NORMALMAP         = _normalMapShader.FindKernel("Calculate_NormalMap");


        // CREATE TEXTURES
        _h0               = OceanManager.CreateRenderTexture(_N, RenderTextureFormat.ARGBFloat);
        _hkt_dy           = OceanManager.CreateRenderTexture(_N, RenderTextureFormat.ARGBFloat);
        _hkt_dx           = OceanManager.CreateRenderTexture(_N, RenderTextureFormat.ARGBFloat);
        _hkt_dz           = OceanManager.CreateRenderTexture(_N, RenderTextureFormat.ARGBFloat);
        _butterfly        = PrecomputeTwiddleFactorsAndInputIndices();
        _pingpong0        = OceanManager.CreateRenderTexture(_N, RenderTextureFormat.ARGBFloat);
        _pingpong1        = OceanManager.CreateRenderTexture(_N, RenderTextureFormat.ARGBFloat);


        _buffer_dy        = OceanManager.CreateRenderTexture(_N, RenderTextureFormat.ARGBFloat);
        _buffer_dx        = OceanManager.CreateRenderTexture(_N, RenderTextureFormat.ARGBFloat);
        _buffer_dz        = OceanManager.CreateRenderTexture(_N, RenderTextureFormat.ARGBFloat);
        _displacements_dy = OceanManager.CreateRenderTexture(_N, RenderTextureFormat.ARGBFloat, FilterMode.Trilinear);
        _displacements_dx = OceanManager.CreateRenderTexture(_N, RenderTextureFormat.ARGBFloat, FilterMode.Trilinear);
        _displacements_dz = OceanManager.CreateRenderTexture(_N, RenderTextureFormat.ARGBFloat, FilterMode.Trilinear);
        _displacements    = OceanManager.CreateRenderTexture(_N, RenderTextureFormat.ARGBFloat, FilterMode.Trilinear);

        _normals          = OceanManager.CreateRenderTexture(_N, RenderTextureFormat.ARGBFloat, FilterMode.Trilinear);
    }

    public void Calculate_h0(OceanSettings oceanSettings, List<Texture2D> _Noises) {
        // Set Variables
        _h0Shader.SetInt("N", _N);                                          // Resolution
        _h0Shader.SetFloat("L", oceanSettings._L);                        // PatchSize
        _h0Shader.SetFloat("A", oceanSettings._A);                          // Amplitude
        _h0Shader.SetFloat("windSpeed", oceanSettings._WindSpeed);
        _h0Shader.SetVector("windDirection", oceanSettings._WindDirection);

        //// Set Textures
        //Texture2D _Noises0 = oceanManager.GetNoiseTexture(_N, 0);
        //Texture2D _Noises1 = oceanManager.GetNoiseTexture(_N, 1);
        //Texture2D _Noises2 = oceanManager.GetNoiseTexture(_N, 2);
        //Texture2D _Noises3 = oceanManager.GetNoiseTexture(_N, 3);

        _h0Shader.SetTexture(KERNEL_OCEAN_H0, "noise_r0", _Noises[0]);
        _h0Shader.SetTexture(KERNEL_OCEAN_H0, "noise_i0", _Noises[1]);
        _h0Shader.SetTexture(KERNEL_OCEAN_H0, "noise_r1", _Noises[2]);
        _h0Shader.SetTexture(KERNEL_OCEAN_H0, "noise_i1", _Noises[3]);
        _h0Shader.SetTexture(KERNEL_OCEAN_H0, "h0", _h0);
        
        // Execute Shader
        _h0Shader.Dispatch(KERNEL_OCEAN_H0, _N / LOCAL_WORK_GROUPS_X, _N / LOCAL_WORK_GROUPS_Y, 1);
    }

    public void Calculate_ht(OceanSettings oceanSettings) {
        // Set Variables
        _htShader.SetInt("N", _N);                  // Resolution
        _htShader.SetFloat("L", oceanSettings._L);  // PatchSize
        _htShader.SetFloat("t", Time.time);         // Time

        // Set Textures
        _htShader.SetTexture(KERNEL_OCEAN_HT, "h0", _h0);
        _htShader.SetTexture(KERNEL_OCEAN_HT, "hkt_dy", _hkt_dy);
        _htShader.SetTexture(KERNEL_OCEAN_HT, "hkt_dx", _hkt_dx);
        _htShader.SetTexture(KERNEL_OCEAN_HT, "hkt_dz", _hkt_dz);

        // Execute Shader
        _htShader.Dispatch(KERNEL_OCEAN_HT, _N / LOCAL_WORK_GROUPS_X, _N / LOCAL_WORK_GROUPS_Y, 1);
    }


    private int pingPong = 1;
    public void Calculate_FFT(RenderTexture input, RenderTexture output) {
        int logSize = (int)Mathf.Log(_N, 2);
        pingPong = 1;

        Graphics.Blit(input, _pingpong0);
        _fftShader.SetTexture(KERNEL_HORIZONTAL_STEP_IFFT, "butterfly", _butterfly);
        _fftShader.SetTexture(KERNEL_HORIZONTAL_STEP_IFFT, "pingpong0", _pingpong0);
        _fftShader.SetTexture(KERNEL_HORIZONTAL_STEP_IFFT, "pingpong1", _pingpong1);
        for (int i = 0; i < logSize; i++) {
            pingPong = (1 - pingPong);
            _fftShader.SetInt("stage", i);
            _fftShader.SetInt("pingpong", pingPong);
            _fftShader.Dispatch(KERNEL_HORIZONTAL_STEP_IFFT, _N / LOCAL_WORK_GROUPS_X, _N / LOCAL_WORK_GROUPS_Y, 1);
        }

        //pingPong = 1;
        _fftShader.SetTexture(KERNEL_VERTICAL_STEP_IFFT, "butterfly", _butterfly);
        _fftShader.SetTexture(KERNEL_VERTICAL_STEP_IFFT, "pingpong0", _pingpong0);
        _fftShader.SetTexture(KERNEL_VERTICAL_STEP_IFFT, "pingpong1", _pingpong1);
        for (int i = 0; i < logSize; i++) {
            pingPong = (1 - pingPong);
            _fftShader.SetInt("stage", i);
            _fftShader.SetInt("pingpong", pingPong);
            _fftShader.Dispatch(KERNEL_VERTICAL_STEP_IFFT, _N / LOCAL_WORK_GROUPS_X, _N / LOCAL_WORK_GROUPS_Y, 1);
        }

        if (pingPong == 0) Graphics.Blit(_pingpong1, output);
        Graphics.Blit(_pingpong0, output);
    }

    public void Calculate_FinalPass(RenderTexture input, RenderTexture output) {
        // Set Variables
        _finalPassShader.SetInt("N", _N);
        _finalPassShader.SetInt("pingpong", pingPong);

        // Set Textures
        _finalPassShader.SetTexture(KERNEL_OCEAN_FINALPASS, "input", input);
        _finalPassShader.SetTexture(KERNEL_OCEAN_FINALPASS, "output", output);

        // Execute Shader
        _finalPassShader.Dispatch(KERNEL_OCEAN_FINALPASS, _N / LOCAL_WORK_GROUPS_X, _N / LOCAL_WORK_GROUPS_Y, 1);
    }

    public void Merge_Displacements(RenderTexture displacement_dy, RenderTexture displacement_dx, RenderTexture displacement_dz, RenderTexture output) {
        // Set Textures
        _finalPassShader.SetTexture(KERNEL_OCEAN_MERGEDISPLACEMENT, "displacement_dy", displacement_dy);
        _finalPassShader.SetTexture(KERNEL_OCEAN_MERGEDISPLACEMENT, "displacement_dx", displacement_dx);
        _finalPassShader.SetTexture(KERNEL_OCEAN_MERGEDISPLACEMENT, "displacement_dz", displacement_dz);
        _finalPassShader.SetTexture(KERNEL_OCEAN_MERGEDISPLACEMENT, "output", output);

        // Execute Shader
        _finalPassShader.Dispatch(KERNEL_OCEAN_MERGEDISPLACEMENT, _N / LOCAL_WORK_GROUPS_X, _N / LOCAL_WORK_GROUPS_Y, 1);
    }

    public void Calculate_Normals(RenderTexture input, RenderTexture output) {
        // Set Variables
        _normalMapShader.SetInt("N", _N);

        // Set Textures
        _normalMapShader.SetTexture(KERNEL_OCEAN_NORMALMAP, "displacementMap", input);
        _normalMapShader.SetTexture(KERNEL_OCEAN_NORMALMAP, "normalMap", output);

        // Execute Shader
        _normalMapShader.Dispatch(KERNEL_OCEAN_NORMALMAP, _N / LOCAL_WORK_GROUPS_X, _N / LOCAL_WORK_GROUPS_Y, 1);
    }

    private RenderTexture PrecomputeTwiddleFactorsAndInputIndices() {
        int logSize = (int)Mathf.Log(_N, 2);
        RenderTexture rt = new RenderTexture(logSize, _N, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        rt.filterMode = FilterMode.Point;
        rt.wrapMode = TextureWrapMode.Repeat;
        rt.enableRandomWrite = true;
        rt.Create();

        // Set Variables
        _butterflyShader.SetInt("N", _N); // Resolution

        // Set Buffer
        ComputeBuffer buffer = null;
        switch (_N) {
            case 128:
                buffer = new ComputeBuffer(bit_reversed_128.Length, sizeof(int));
                buffer.SetData(bit_reversed_128);
                break;

            case 256:
                buffer = new ComputeBuffer(bit_reversed_256.Length, sizeof(int));
                buffer.SetData(bit_reversed_256);
                break;

            case 512:
                buffer = new ComputeBuffer(bit_reversed_512.Length, sizeof(int));
                buffer.SetData(bit_reversed_512);
                break;

            case 1024:
                buffer = new ComputeBuffer(bit_reversed_1024.Length, sizeof(int));
                buffer.SetData(bit_reversed_1024);
                break;
        }
        _butterflyShader.SetBuffer(KERNEL_BUTTERFLYTEXTURE, "bit_reversed", buffer);

        // Set Textures
        _butterflyShader.SetTexture(KERNEL_BUTTERFLYTEXTURE, "butterfly", rt);

        // Execute Shader
        _butterflyShader.Dispatch(KERNEL_BUTTERFLYTEXTURE, logSize, _N, 1);

        buffer.Release();
        return rt;
    }

    // ------------------------------------------------------------------------------------

    int[] bit_reversed_1024 = {
        0, 512, 256, 768, 128, 640, 384, 896, 64, 576, 320, 832, 192, 704, 448, 960,
        32, 544, 288, 800, 160, 672, 416, 928, 96, 608, 352, 864, 224, 736, 480, 992,
        16, 528, 272, 784, 144, 656, 400, 912, 80, 592, 336, 848, 208, 720, 464, 976,
        48, 560, 304, 816, 176, 688, 432, 944, 112, 624, 368, 880, 240, 752, 496,
        1008, 8, 520, 264, 776, 136, 648, 392, 904, 72, 584, 328, 840, 200, 712, 456,
        968, 40, 552, 296, 808, 168, 680, 424, 936, 104, 616, 360, 872, 232, 744,
        488, 1000, 24, 536, 280, 792, 152, 664, 408, 920, 88, 600, 344, 856, 216, 728,
        472, 984, 56, 568, 312, 824, 184, 696, 440, 952, 120, 632, 376, 888, 248, 760,
        504, 1016, 4, 516, 260, 772, 132, 644, 388, 900, 68, 580, 324, 836, 196, 708,
        452, 964, 36, 548, 292, 804, 164, 676, 420, 932, 100, 612, 356, 868, 228, 740,
        484, 996, 20, 532, 276, 788, 148, 660, 404, 916, 84, 596, 340, 852, 212, 724,
        468, 980, 52, 564, 308, 820, 180, 692, 436, 948, 116, 628, 372, 884, 244, 756,
        500, 1012, 12, 524, 268, 780, 140, 652, 396, 908, 76, 588, 332, 844, 204, 716,
        460, 972, 44, 556, 300, 812, 172, 684, 428, 940, 108, 620, 364, 876, 236, 748,
        492, 1004, 28, 540, 284, 796, 156, 668, 412, 924, 92, 604, 348, 860, 220, 732,
        476, 988, 60, 572, 316, 828, 188, 700, 444, 956, 124, 636, 380, 892, 252, 764,
        508, 1020, 2, 514, 258, 770, 130, 642, 386, 898, 66, 578, 322, 834, 194, 706,
        450, 962, 34, 546, 290, 802, 162, 674, 418, 930, 98, 610, 354, 866, 226, 738,
        482, 994, 18, 530, 274, 786, 146, 658, 402, 914, 82, 594, 338, 850, 210, 722,
        466, 978, 50, 562, 306, 818, 178, 690, 434, 946, 114, 626, 370, 882, 242, 754,
        498, 1010, 10, 522, 266, 778, 138, 650, 394, 906, 74, 586, 330, 842, 202, 714,
        458, 970, 42, 554, 298, 810, 170, 682, 426, 938, 106, 618, 362, 874, 234, 746,
        490, 1002, 26, 538, 282, 794, 154, 666, 410, 922, 90, 602, 346, 858, 218, 730,
        474, 986, 58, 570, 314, 826, 186, 698, 442, 954, 122, 634, 378, 890, 250, 762, 
        506, 1018, 6, 518, 262, 774, 134, 646, 390, 902, 70, 582, 326, 838, 198, 710,
        454, 966, 38, 550, 294, 806, 166, 678, 422, 934, 102, 614, 358, 870, 230, 742,
        486, 998, 22, 534, 278, 790, 150, 662, 406, 918, 86, 598, 342, 854, 214, 726,
        470, 982, 54, 566, 310, 822, 182, 694, 438, 950, 118, 630, 374, 886, 246, 758,
        502, 1014, 14, 526, 270, 782, 142, 654, 398, 910, 78, 590, 334, 846, 206, 718,
        462, 974, 46, 558, 302, 814, 174, 686, 430, 942, 110, 622, 366, 878, 238, 750,
        494, 1006, 30, 542, 286, 798, 158, 670, 414, 926, 94, 606, 350, 862, 222, 734,
        478, 990, 62, 574, 318, 830, 190, 702, 446, 958, 126, 638, 382, 894, 254, 766,
        510, 1022, 1, 513, 257, 769, 129, 641, 385, 897, 65, 577, 321, 833, 193, 705,
        449, 961, 33, 545, 289, 801, 161, 673, 417, 929, 97, 609, 353, 865, 225, 737,
        481, 993, 17, 529, 273, 785, 145, 657, 401, 913, 81, 593, 337, 849, 209, 721, 
        465, 977, 49, 561, 305, 817, 177, 689, 433, 945, 113, 625, 369, 881, 241, 753,
        497, 1009, 9, 521, 265, 777, 137, 649, 393, 905, 73, 585, 329, 841, 201, 713,
        457, 969, 41, 553, 297, 809, 169, 681, 425, 937, 105, 617, 361, 873, 233, 745,
        489, 1001, 25, 537, 281, 793, 153, 665, 409, 921, 89, 601, 345, 857, 217, 729,
        473, 985, 57, 569, 313, 825, 185, 697, 441, 953, 121, 633, 377, 889, 249, 761,
        505, 1017, 5, 517, 261, 773, 133, 645, 389, 901, 69, 581, 325, 837, 197, 709,
        453, 965, 37, 549, 293, 805, 165, 677, 421, 933, 101, 613, 357, 869, 229, 741,
        485, 997, 21, 533, 277, 789, 149, 661, 405, 917, 85, 597, 341, 853, 213, 725,
        469, 981, 53, 565, 309, 821, 181, 693, 437, 949, 117, 629, 373, 885, 245, 757,
        501, 1013, 13, 525, 269, 781, 141, 653, 397, 909, 77, 589, 333, 845, 205, 717,
        461, 973, 45, 557, 301, 813, 173, 685, 429, 941, 109, 621, 365, 877, 237, 749,
        493, 1005, 29, 541, 285, 797, 157, 669, 413, 925, 93, 605, 349, 861, 221, 733,
        477, 989, 61, 573, 317, 829, 189, 701, 445, 957, 125, 637, 381, 893, 253, 765,
        509, 1021, 3, 515, 259, 771, 131, 643, 387, 899, 67, 579, 323, 835, 195, 707,
        451, 963, 35, 547, 291, 803, 163, 675, 419, 931, 99, 611, 355, 867, 227, 739,
        483, 995, 19, 531, 275, 787, 147, 659, 403, 915, 83, 595, 339, 851, 211, 723,
        467, 979, 51, 563, 307, 819, 179, 691, 435, 947, 115, 627, 371, 883, 243, 755,
        499, 1011, 11, 523, 267, 779, 139, 651, 395, 907, 75, 587, 331, 843, 203, 715,
        459, 971, 43, 555, 299, 811, 171, 683, 427, 939, 107, 619, 363, 875, 235, 747,
        491, 1003, 27, 539, 283, 795, 155, 667, 411, 923, 91, 603, 347, 859, 219, 731,
        475, 987, 59, 571, 315, 827, 187, 699, 443, 955, 123, 635, 379, 891, 251, 763,
        507, 1019, 7, 519, 263, 775, 135, 647, 391, 903, 71, 583, 327, 839, 199, 711,
        455, 967, 39, 551, 295, 807, 167, 679, 423, 935, 103, 615, 359, 871, 231, 743,
        487, 999, 23, 535, 279, 791, 151, 663, 407, 919, 87, 599, 343, 855, 215, 727,
        471, 983, 55, 567, 311, 823, 183, 695, 439, 951, 119, 631, 375, 887, 247, 759,
        503, 1015, 15, 527, 271, 783, 143, 655, 399, 911, 79, 591, 335, 847, 207, 719,
        463, 975, 47, 559, 303, 815, 175, 687, 431, 943, 111, 623, 367, 879, 239, 751,
        495, 1007, 31, 543, 287, 799, 159, 671, 415, 927, 95, 607, 351, 863, 223, 735,
        479, 991, 63, 575, 319, 831, 191, 703, 447, 959, 127, 639, 383, 895, 255, 767,
        511, 1023
    };

    int[] bit_reversed_512 = {
        0, 256, 128, 384, 64, 320, 192, 448, 32, 288, 160, 416, 96, 352, 224, 480,
        16, 272, 144, 400, 80, 336, 208, 464, 48, 304, 176, 432, 112, 368, 240, 496,
        8, 264, 136, 392, 72, 328, 200, 456, 40, 296, 168, 424, 104, 360, 232, 488,
        24, 280, 152, 408, 88, 344, 216, 472, 56, 312, 184, 440, 120, 376, 248, 504,
        4, 260, 132, 388, 68, 324, 196, 452, 36, 292, 164, 420, 100, 356, 228, 484,
        20, 276, 148, 404, 84, 340, 212, 468, 52, 308, 180, 436, 116, 372, 244, 500,
        12, 268, 140, 396, 76, 332, 204, 460, 44, 300, 172, 428, 108, 364, 236, 492,
        28, 284, 156, 412, 92, 348, 220, 476, 60, 316, 188, 444, 124, 380, 252, 508,
        2, 258, 130, 386, 66, 322, 194, 450, 34, 290, 162, 418, 98, 354, 226, 482,
        18, 274, 146, 402, 82, 338, 210, 466, 50, 306, 178, 434, 114, 370, 242, 498,
        10, 266, 138, 394, 74, 330, 202, 458, 42, 298, 170, 426, 106, 362, 234, 490,
        26, 282, 154, 410, 90, 346, 218, 474, 58, 314, 186, 442, 122, 378, 250, 506,
        6, 262, 134, 390, 70, 326, 198, 454, 38, 294, 166, 422, 102, 358, 230, 486,
        22, 278, 150, 406, 86, 342, 214, 470, 54, 310, 182, 438, 118, 374, 246, 502,
        14, 270, 142, 398, 78, 334, 206, 462, 46, 302, 174, 430, 110, 366, 238, 494,
        30, 286, 158, 414, 94, 350, 222, 478, 62, 318, 190, 446, 126, 382, 254, 510,
        1, 257, 129, 385, 65, 321, 193, 449, 33, 289, 161, 417, 97, 353, 225, 481,
        17, 273, 145, 401, 81, 337, 209, 465, 49, 305, 177, 433, 113, 369, 241, 497,
        9, 265, 137, 393, 73, 329, 201, 457, 41, 297, 169, 425, 105, 361, 233, 489,
        25, 281, 153, 409, 89, 345, 217, 473, 57, 313, 185, 441, 121, 377, 249, 505,
        5, 261, 133, 389, 69, 325, 197, 453, 37, 293, 165, 421, 101, 357, 229, 485,
        21, 277, 149, 405, 85, 341, 213, 469, 53, 309, 181, 437, 117, 373, 245, 501,
        13, 269, 141, 397, 77, 333, 205, 461, 45, 301, 173, 429, 109, 365, 237, 493,
        29, 285, 157, 413, 93, 349, 221, 477, 61, 317, 189, 445, 125, 381, 253, 509,
        3, 259, 131, 387, 67, 323, 195, 451, 35, 291, 163, 419, 99, 355, 227, 483,
        19, 275, 147, 403, 83, 339, 211, 467, 51, 307, 179, 435, 115, 371, 243, 499,
        11, 267, 139, 395, 75, 331, 203, 459, 43, 299, 171, 427, 107, 363, 235, 491,
        27, 283, 155, 411, 91, 347, 219, 475, 59, 315, 187, 443, 123, 379, 251, 507,
        7, 263, 135, 391, 71, 327, 199, 455, 39, 295, 167, 423, 103, 359, 231, 487,
        23, 279, 151, 407, 87, 343, 215, 471, 55, 311, 183, 439, 119, 375, 247, 503,
        15, 271, 143, 399, 79, 335, 207, 463, 47, 303, 175, 431, 111, 367, 239, 495,
        31, 287, 159, 415, 95, 351, 223, 479, 63, 319, 191, 447, 127, 383, 255, 511
    };

    int[] bit_reversed_256 = { 
        0, 128, 64, 192, 32, 160, 96, 224, 16, 144, 80, 208, 48,
        176, 112, 240, 8, 136, 72, 200, 40, 168, 104, 232, 24, 152, 88, 216, 56,
        184, 120, 248, 4, 132, 68, 196, 36, 164, 100, 228, 20, 148, 84, 212, 52,
        180, 116, 244, 12, 140, 76, 204, 44, 172, 108, 236, 28, 156, 92, 220, 60,
        188, 124, 252, 2, 130, 66, 194, 34, 162, 98, 226, 18, 146, 82, 210, 50, 178,
        114, 242, 10, 138, 74, 202, 42, 170, 106, 234, 26, 154, 90, 218, 58, 186,
        122, 250, 6, 134, 70, 198, 38, 166, 102, 230, 22, 150, 86, 214, 54, 182, 118,
        246, 14, 142, 78, 206, 46, 174, 110, 238, 30, 158, 94, 222, 62, 190, 126,
        254, 1, 129, 65, 193, 33, 161, 97, 225, 17, 145, 81, 209, 49, 177, 113, 241,
        9, 137, 73, 201, 41, 169, 105, 233, 25, 153, 89, 217, 57, 185, 121, 249, 5,
        133, 69, 197, 37, 165, 101, 229, 21, 149, 85, 213, 53, 181, 117, 245, 13,
        141, 77, 205, 45, 173, 109, 237, 29, 157, 93, 221, 61, 189, 125, 253, 3, 131,
        67, 195, 35, 163, 99, 227, 19, 147, 83, 211, 51, 179, 115, 243, 11, 139, 75,
        203, 43, 171, 107, 235, 27, 155, 91, 219, 59, 187, 123, 251, 7, 135, 71, 199,
        39, 167, 103, 231, 23, 151, 87, 215, 55, 183, 119, 247, 15, 143, 79, 207, 47,
        175, 111, 239, 31, 159, 95, 223, 63, 191, 127, 255 
    };

    int[] bit_reversed_128 = {
        0, 64, 32, 96, 16, 80, 48, 112, 8, 72, 40, 104, 24, 88, 56, 120, 4, 68, 36, 
        100, 20, 84, 52, 116, 12, 76, 44, 108, 28, 92, 60, 124, 2, 66, 34, 98, 18, 82,
        50, 114, 10, 74, 42, 106, 26, 90, 58, 122, 6, 70, 38, 102, 22, 86, 54, 118,
        14, 78, 46, 110, 30, 94, 62, 126, 1, 65, 33, 97, 17, 81, 49, 113, 9, 73, 41,
        105, 25, 89, 57, 121, 5, 69, 37, 101, 21, 85, 53, 117, 13, 77, 45, 109, 29, 93, 
        61, 125, 3, 67, 35, 99, 19, 83, 51, 115, 11, 75, 43, 107, 27, 91, 59, 123,
        7, 71, 39, 103, 23, 87, 55, 119, 15, 79, 47, 111, 31, 95, 63, 127
    };
}
