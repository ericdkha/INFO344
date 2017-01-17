<?php

# Includes PHP file to use its functions
include_once("/srv/www/htdocs/common.php");
top();
# Checks to make sure the user did not input nothing, relocates to error page if so
if ($_GET['name'] == '') {
    header("Location: error.php");
    die();
}


$playerName = '';

# Checks if multiple names were returned
if ($nRows > 1) {
    ?>

    <div id="player-info">
    <div class="jumbotron container-fluid">
        <h1>Please specify the player you want</h1>
    </div>
    <div id="search-bar">
        <form class='form' action="search.php" method="get">
            <input name="name" type="text" size="20" placeholder="Enter Player's Name" autofocus="autofocus" />
            <input type="submit" value="go" />
        </form>
    </div>
    <div>
        <table class="table-fill2">
            <th>Player</th><th>Team</th>
    <?php
    foreach ($players as $player) {
        ?>


        <tdbody>
        <?php
        printNames($player);
    }
    ?>

        </tdbody>
        </table>

    <?php
    bottom();
    die();
}

# Gets the name of the player returned a stores their stats in a variable
foreach ($players as $player) {
    $playerName = $player["name"];
    $selected = $player;
}

# checks to see if a player was found
if ($playerName == '') { # prints name not found message if not
    header("Location: error.php");
    die();
}
?>

    <div id="player-info">
        <div class="jumbotron container-fluid">
            <h1><?= $playerName ?></h1>
            <h2>Plays on the <?= $selected["team"] ?></a></h2>
    <!--            <p>{{player.age}} years old, Height: {{player.height}}'', Weight: {{player.weight}}lbs, Shoots: {{player.hand}}</p>-->
        </div>
        <div id="search-bar">
            <form class='form' action="search.php" method="get">
                <input name="name" type="text" size="20" placeholder="Enter Player's Name" autofocus="autofocus" />
                <input type="submit" value="go" />
            </form>
        </div>

<?php
printResults($selected);
bottom();
?>