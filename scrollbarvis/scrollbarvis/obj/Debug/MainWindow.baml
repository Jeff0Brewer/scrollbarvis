   M S B A M L   `   `   `  ����G  Cscrollbarvis, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null+clr-namespace:scrollbarvisscrollbarvis  5   	       scrollbarvis.MainWindow   5   	   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   ,localclr-namespace:scrollbarvis     5   	   -   5          � Title$  
MainWindow��5   	    � WindowState$ 	Maximized=�6    � 	Resources 5
      ��%    6   .��5      �� -   5        $
��canv��5      .+�5   
   �� -   5        $��bg��5      $
��1366��6   $	��740��6+   
 �� Top$ 0��69   ��5      !� 5       !� Visual 5      r� 5      -   5         r�Name$ cloudLecture��5   '    r� Source$ ../../images/clouds.mp4?�6=    r� LoadedBehavior$ Manual=�6^    r� Stretch$
 Fill=�6v   	 J� Opacity$		 1.0Q�6�   6�   5      5      �� 5   
   -   5        $��PlaybackSlider��5      
 �� Right$
 10��6'    �� Bottom$ 0��69    K� ZIndex$
 1000��6K    � Minimum$ 0Q�6_    � Maximum$	 100Q�6k    �� TickFrequency$ 5Q�5       �� IsSnapToTickEnabled . $	��250��5      $��20��6    �� TickPlacement$ BottomRight=�6)   
����  �$��10q�6W   �� 5   
   -   5        $��AnimatePlay��5      $	
 315��6$   $ 5��67   $
 1000��6I   $��30��6]   $��30��6h   ��5      �� 5      .R�5      �� $��30��6    $��30��6+   ��5       �� 5!       j� Stretch$
 Fill=�6)    �� ImageSource$ images/play_green.png��68   6^   5#      5$      5&      �� 5'   
   -   5        $��AnimatePause��5'      $	
 315��6%   $ 5��68   $
 1000��6J   $��30��6^   $��30��6i   $y�Hidden=�6u   ��5(      �� 5)      .R�5*      �� $��30��6    $��30��6+   ��5+      �� 5,      $
 Fill=�6)   $ images/pause_green.png��68   6_   5.      5/      51      �� 52   
   -   5        $��Clear_Animate��52      $	
 280��6&   $ 5��69   $
 1000��6K   $��30��6_   $��30��6j   ��53      �� 54      .R�55      �� $��25��6    $��25��6+   ��56      �� 57      $
 Fill=�6)   $ images/stop_red.png��68   6\   59      5:      5<      �� 5=   
   -	   5        $��HeatmapViz_Button��5=       �� Left$ 10��6*   $ 10��6;   $
 1000��6N   $��60��6b   $��30��6m   ��5>      �� 5?      $��	Post-Test��6   6/   5A      5B      5C      