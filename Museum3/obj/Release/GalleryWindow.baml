   M S B A M L   `   `   `  ���� B  >Museum3, Version=1.1.0.0, Culture=neutral, PublicKeyToken=null!clr-namespace:Museum3Museum3      Museum3.GalleryWindow   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     'localclr-namespace:Museum3       � Title$  GalleryWindow��$
��1080��$
��1920�� � WindowStartupLocation$ CenterScreen=� � WindowState$ 	Maximized=� � WindowStyle$
 None=�
���   � � AllowsTransparency .   � 	Resources    
CintGost63   Museum3.CenterConverter  CenterConverter  ZoomSliderStyle �� 
TargetType �� Property �� Value	 �� 
TargetType
 ��Name �� CornerRadius J� IsHitTestVisible ]�Name ]� DecreaseRepeatButton J� Opacity ]� Thumb ]� IncreaseRepeatButton��%�  &	        &	 3     &	 8     *�-)pack://application:,,,/Fonts/#Cint_Gost63)�  �� " ��.���  � �� �� "	 ��.R�� $��48��$��0q�.+��� $
 TrackBackground��-   ���	#77CCCCCC$��14��$��Center=�$ 2��$
��16,0q� . ]� $ 
PART_Track��$��Center=�$
��16,0q�$��20�� ��  .  $ 0Q���Z�  n� $��44��$��24��$��Center=����� "	 n�.R��� $��44��$��24�����	#CCDDDDDD���#AAAAAA$��1q�$ 12�� ��  .  $ 0Q���Z� .��� -    �Name$ MainGrid��
���   �.+��� 
���   � K� ZIndex$ 0���� -    ��Name$ 
FullZIndex��
���   �.+�r� -   $��	GearsIdle�� r� LoadedBehavior$ Manual=� r� UnloadedBehavior$
 Stop=� r� Stretch$ Uniform=� r� ScrubbingEnabled . $
��1920��$
��1080���� -   $ BlackBackFull��$	��970��$	��735�� �� Left$	 440��
 �� Top$	 120��
���   ��� -    ��Name$ ImageDisplay��$��Uniform=�$ 0Q�$	��970��$	��735��$	 440��$	 120���� -   $ StartImageDisplay��$��UniformToFill=�$ 1Q�$0��*pack://application:,,,/Sprites/fonDesc.png��$	��970��$	��735��$	 440��$	 120���� $��Uniform=�$/��)pack://application:,,,/Sprites/shadow.png��$
��1920��$��
0, 0, 0, 0q�$ 10�� .  �� $��Uniform=�$��Center=�$
��Left=�$3��-pack://application:,,,/Sprites/ViewerMain.png��$
��1550��$��70, 0, 0, 0q�$ 6�� .  �� $��Uniform=�$��Center=�$4��.pack://application:,,,/Sprites/ViewerPart3.png��$
��1100��$��20, 910, 0, 0q�$ 6�� .  �� .+�r� -    r�Name$ 
ButtonNext��$ Manual=�$ Uniform=�$��89��$	 446��$	 951�� r� 
SpeedRatio$	 2.0Q�r� -	   $ 
ButtonPrev��$ Manual=�$ Uniform=�$��92��$
 1335��$	 953��$	 2.0Q��� -
   $ 	ImageDesc��$ 10��.+��� -   $ ImageDescBackground��$	��625��$��1��$	 590��$	 970��$��12q�����  �� ImageSource$0 *pack://application:,,,/Sprites/fonDesc.png���� -   $ ImageDescFrame��$��UniformToFill=�$4��.pack://application:,,,/Sprites/ViewerPart1.png��$	��650��$��17��$	 590��$	 970���� -   $��TextBlockDescImage��$	��615��$��1��$����PostgreSQL — мощная, бесплатная СУБД с поддержкой ACID, JSON, репликации и расширяемости.�� �� TextWrapping$
 Wrap=�#��[  
�������$��10, 13q�$��0q�  �� 
IsReadOnly  . 
������ $	 607��$	 980���� $ 5��.+�r� -   $ ImageDescButton��$ Manual=�$ Uniform=�$	��180��$	 120��$	 805��$ 2Q�r� -   $ ImageDescButtonRevers��$ Manual=�$ Uniform=�$	��180��$	 120��$	 805��$ 2Q�r� -   $ 
ButtonZoom��$ Manual=�$ Uniform=�$	��160��$	 125��$	 540��$ 2Q�r� -   $ ImageShowButton��$ Manual=�$ Uniform=�$	��180��$	 139��$	 235��$ 2Q�r� -   $ ImageShowButtonRevers��$ Manual=�$ Uniform=�$	��180��$	 139��$	 235��$ 2Q��� -   $��scrollViewer��$��Hidden=�$��Center=�$��Center=�$	��587��$	��350��$��0, 20, -1558, 0q�.���� -   $��SliderContainer������ .R��� ! �� Orientation$! Vertical=����� .R�� -   $	��117��$��74��.+��� $0��*pack://application:,,,/Sprites/fonDesc.png��$	��117��$��64��$��Uniform=��� $3��-pack://application:,,,/Sprites/PhotoFrame.png��$	��117��$��74��$��Uniform=��� $��97��$��64��$��UniformToFill=�$ 0Q����� *Path+y��� *
Visibility+���� " ��" �� Triggers" �� # �� Binding# �� *
Visibility+$ �� Value$ Visible% N� EnterActions% �� .���� .��W� & �� TargetProperty$& Opacity�' W� From$' 0X�	( W� To$( 1X�) g� Duration$) 0:0:0.4D�* g� FillBehavior$* HoldEnd=��� -   $ TouchCanvas��$	��270��$��Right=�$��0, 0, 50, 0q�.+��� -   $ FrameMiniFoto��$��Uniform=� .  $4��.pack://application:,,,/Sprites/ViewerPart2.png��$	��170��$ 523.5��$ 25���� -   $ ButtonClose��$<��6pack://application:,,,/Sprites/GearsParts_Button_2.png��$��Uniform=�$	��275��$��80��$��Right=�$��Bottom=�$��0, 0, 45, 110q��� -   $ 	BlackBack��$
��1920��$
��1080��$ 9��
���   �$y�Hidden=��� -   $ ImageFullScreen��$y�Hidden=�$��Uniform=�+ J� RenderTransformOrigin$+ 0.5,0.5&�, J� IsManipulationEnabled, . $ 10���� -   - ��Name$- 
ZoomSlider��$ 11��#��[ . � Minimum$	. 1.0Q�/ � Maximum$	/ 4.0Q�0 � Value$	0 1.0Q�$
��1400��$��Bottom=�$��0, 0, 0, 50q�$��48��$y�	Collapsed=��� -   $ 	CloseZoom��$��Uniform=�$ 11��$��30��$��30��$��Right=�$	��Top=�$��15q�$.��(pack://application:,,,/Sprites/close.png��$y�Hidden=��� $5��/pack://application:,,,/Sprites/NameCategory.png��$	��150��$��Uniform=�$��Right=�$	��Top=�$��0, 180, 87, 0q�� $	��Top=�$��Right=�$	��115��$��40��$��0, 187, 105, 0q�.+��� -   1 ��Name$1 NameCategory��$C��=Очень длинное название категории��$��14(�$
��Wrap=�2 �� TextAlignment$2 Center=�$��Center=�$��Center=�#��[  
�������