<?php

# Includes PHP file to use its functions
include_once("/srv/www/htdocs/common.php");
header("Content type: text/html");
top();
$idk = searchPlayers();
?>

<div id="player-info">
    <div class="jumbotron container-fluid">
        <h1>Please specify the player you want</h1>
        <h2><?= var_dump($idk) ?></h2>
    </div>
    <div id="search-bar">
        <form class='form' action="search.php" method="get">
            <input name="name" type="text" size="20" placeholder="Enter Player's Name" autofocus="autofocus" />
            <input type="submit" value="go" />
        </form>
    </div>


<?php
printNames($idk);
bottom();
?>