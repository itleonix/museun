   M S B A M L   `   `   `  ���� B  >Museum3, Version=1.1.0.0, Culture=neutral, PublicKeyToken=null!clr-namespace:Museum3Museum3      Museum3.MainWindow   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     'localclr-namespace:Museum3       � Title$  
MainWindow��$	��450��$	��800�� � WindowState$ 	Maximized=� � WindowStyle$
 None=�����  �� ImageSource$, &pack://application:,,,/Sprites/fon.png�� � 	Resources    
CintGost63��%=   &	        *�-)pack://application:,,,/Fonts/#Cint_Gost63)�.���� $��Hidden=�$��Hidden=�.��� .+�� -    �Name$ ContainersGrid��$��210, 110, 150, 160q��� -    ��Name$ 
BackSlider��
������  J� IsHitTestVisible .  .+��� -    ��Name$ SliderFotoBack��$2��,pack://application:,,,/Sprites/BlackBack.png��$
��1500��$	��790��$��0, 15, 0, 0q�$��UniformToFill=�	 J� Opacity$	 1Q�
 �� Left$	
 230��
 �� Top$
 -745�� .  �� -   $ 
SliderFoto��$
��1500��$	��790��$��0, 15, 0, 0q�$��Uniform=�$	 1Q�$	
 230��$
 -745�� .  ��  .  .+��� -   $ SliderFrame��$4��.pack://application:,,,/Sprites/Sliderframe.png��$
��1550��$	��830��$��
0, 0, 0, 0q�$
��Fill=� .  $
 -730��$	
 210���� .+�r� -    r�Name$ AlbumUp�� r� LoadedBehavior$ Manual=� r� Stretch$ Uniform=�$
��1920��$	��813��$

 27.5��$	 105��$	 0Q� r� UnloadedBehavior$
 Stop=���  K� ZIndex$ 2��.+��� $/��)pack://application:,,,/Sprites/plank1.png��$��UniformToFill=�$��12��$	��630��$

 1747��$	 140���� .+��� -   $ 
LeftPlank1��$/��)pack://application:,,,/Sprites/plank2.png��$
��Fill=�$	��760��$	��215��$

 -550��$	 330���� -   $ 
LeftPlank2��$/��)pack://application:,,,/Sprites/plank2.png��$
��Fill=�$	��760��$	��215��$

 -550��$	 735���� -	   $ RightPlank1��$/��)pack://application:,,,/Sprites/plank3.png��$
��Fill=�$	��770��$	��215��$

 1750��$	 330���� -
   $ RightPlank2��$/��)pack://application:,,,/Sprites/plank3.png��$
��Fill=�$	��770��$	��215��$

 1750��$	 735���� $9��3pack://application:,,,/Sprites/background_frame.png��$
��1920��$
��1080��$��0,0,0,0q� .  $ 15���� $ 3��$��10,47,719,206q�.+�   Museum3.VideoRepeat  -     Name$
 Left��$	��410��$	��173��$
 16��$	 100���� $ 3��$��1766,120,0,191q�.+�  -   $ Right��$	��320��$	��105��$
 16��$ 181.5���� $ 5��.+�r� -   $ SliderButton��$ Manual=�$ Uniform=�$	��170��$
 35��$	 685�� r� 
SpeedRatio$ 2Q�$ 5��r� -   $ SliderButtonRevers��$ Manual=�$ Uniform=�$	��170��$
 35��$	 685��$ 2Q�$ 4���� $ 5��.+�r� -   $ ButtonUp��$ Manual=�$ Uniform=�$	��105��$
 1782.2��$	 110��r� -   $ 
ButtonDown��$ Manual=�$ Uniform=�$	��105��$
 1784.3��$	 662���� -   $ 
PassWindow��$+��%pack://application:,,,/Sprites/bL.png��$��Uniform=�$��Bottom=�$
��Left=�$ 16��$	��123��$��25, 0, 0, 39q�