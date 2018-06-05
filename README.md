# scrollbarvis

Gaze recording and vizualization for video lecture task with pre/post-test for cloud identification.

*MainWindow*: Video lecture (~10 min). Bubble vizualization for each student.

*Window1*: Pre/post-test. Heatmap vizualization for each student.

### Notes
- *inputFile* array holds gaze coordinate csv files to display
- Window1 takes longer to load if more files are in *inputFile* (loading heatmaps)
- *Clouds-pointer.mp4* video isn't included
- data files from experiments (gaze coordinates for lecture, pre/post test, and test answers) are located in *scrollbarviz/bin/Debug/gazerecordings*
- record button in top right. For lecture, video starts playing upon record.

### To do
- add slider support for gaze playback
- improve loading time for heatmaps
