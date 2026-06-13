This is the second PR for my project which is a Snake game. <br>

To build it use the command: 
> dotnet build

To run the project and play use:
> dotnet run

Link to the video showing the gameplay:
[Google Drive Link](https://drive.google.com/file/d/1946eBW70aD76KWk9iITvNoj8ROOniNDU/view?usp=sharing)

<br>
This Snake game has three different fruit: apples, pears and bananas.
Apples value 10 points, Pears 20 and Bananas 30. The newly added fruit also have a special ability of extra growth. When the snake eats them it grows by 1 more square for pears and 2 more squares for bananas.
<br>
The game was made to be more visually interesting, adding small stems to the fruit, shadow and a highlighted dot.
The snake's body is more contured, each square having borders. The snake's head is also brighter than the rest of his body.
Initially I planned on adding sprite images to the game but I couldn't manage to make it work without errors which is why
the new design was made only based on drawing.
<br>
The score of the user is also now visible on the screen. When the snake collides with itself or with the wall the user is informed about
being able to restart the game by pressing the Space button.
<br>
Lastly I also included audio. I found and integrated different audio samples for each fruit, which are played whenever the snake eats the fruit and also for the "Game over" condition. This was done using Soundplayer for Windows.

