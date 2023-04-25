# Game Memory Wizard

Playing video games are incredible. Sometimes you miss the good ol'days oh the game shark! A lot of the PC gaming cheat applications that exists today are not the easiest to make your own cheats as they require complex coding knowledge. The Game Memory Wizard aims to be a simple and appoachable way to be able to generate cheats for any *single player* gane.

The Game Memory Wizard works by scanning the game's memory and filtering down to the specific place in memory where the value lives. The application stores the information on the file system to be able to access it at later time.

## Features
- Create Cheat to lock a variable at a specfic value

## Instructions
First execute the Cheat Manager. This will walk the user through creating cheats and to find the specific location in memory of the variable that they want to change.
Next execute the Cheat Executor with the argument of the game name (that is the same game title that the user entered in the Cheat Manager) and it will start you cheats. 

Once the game exits, the Cheat Executor will also exit.

## Special Note:
Because this application reads the memory of the game, it is unable to handle multiplayer games because the memory of a player's health or ammo lives on the server and not the player's computer. Even if you could, don't ruin anyone else's fun. Single Player games only.

# License
This application falls under the MIT License. The software is provided "AS IS", without warranty of any kind. Please see the LINENSE file at the root of the directory to learn more.