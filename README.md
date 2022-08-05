# ImageCropper.WPF
WPF图片裁剪控件，移植自
[WindowsCommunityToolkit.](https://github.com/CommunityToolkit/WindowsCommunityToolkit/tree/main/Microsoft.Toolkit.Uwp.UI.Controls.Media/ImageCropper)
用法与UWP版本一样，UWP支持的属性WPF也支持，并额外增加了两个属性，动画实现方式与UWP不一样，有点拉跨，凑合看。


## 项目结构

- ImageCropper是控件实现库
- ImageCropperDemo仅作演示使用

## 几个说明
- 设置图片可将WriteableBitmap赋值给Source， 如果想直接从文件路径加载，调用方法 ImageCropper.LoadImageFromFile(string imageFile)
清空图片将Source设为null即可，或者ImageCropper.LoadImageFromFile(null)

- CropperEnable 是否允许裁剪， false则隐藏裁剪相关显示，单纯显示图片(新增属性)

- DragImgEnable 是否允许拖动图片(新增属性)
- CroppedRegion 裁剪区域，最后要知道裁剪区域通过此属性拿
- 更多请参考UWP使用


## 示例
```csharp
xaml:

xmlns:imagecropper="clr-namespace:ImageCropper;assembly=ImageCropper"

<imagecropper:ImageCropper
            x:Name="ImageCropperControl"
            CropShape="Rectangular"
            CropperEnable="True"
            DragImgEnable="True"
            ThumbPlacement="All"/>

xaml.cs:

ImageCropperControl.LoadImageFromFile("sine_wave_omega.jpg"));
```

## Demo
![Img](https://github.com/qedsd/ImageCropper.WPF/blob/master/DemoImg/ImageCropper.png?raw=true)

![Img](https://github.com/qedsd/ImageCropper.WPF/blob/master/DemoImg/ImageCropper2.png?raw=true)

![Img](https://github.com/qedsd/ImageCropper.WPF/blob/master/DemoImg/ImageCropper3.png?raw=true)

![Img](https://github.com/qedsd/ImageCropper.WPF/blob/master/DemoImg/ImageCropper4.png?raw=true)
