<?php
/**
 * Put some layout content thats common in all pages, and connection to the database
 */

include("../inc/dbinfo.inc");
$db = new PDO("mysql:dbname=$DB_DATABASE;host=$DB_SERVER;charset=utf8", $DB_USERNAME, $DB_PASSWORD);
$db->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

if (isset($_GET['name'])) {
    $names = $_GET['name'];
//    $names = explode(" ", $name);
//    foreach($names as $name) {
//        $name = ucwords($name);
//        print $name;
//    }
//    $names = implode($names);
    $names = '%' . $names . '%';
    $dbName = $db->quote($names);
//    $names2 = $names . '%';
//    $dbName2 = $db->quote($names2);
//    $names3 = '%' . $names;
//    $dbName3 = $db->quote($names3);
    try {
        # query to retrieve a players that much the given string
        $sql = "SELECT * FROM player p WHERE p.name LIKE $dbName";
        $players = $db->query($sql);
        $nRows = $players->rowCount();

        # Mainly debugging purposes with SQL query
    } catch (PDOException $ex) {
        ?>

        <p>Sorry, a database error occurred. Please try again later.</p>
        <p>(Error details: <?= $ex->getMessage() ?>)</p>

        <?php
    }
}

# Function prints common HTML between pages, specifically the top portion of the page before the result
function top() {
    ?>

    <!DOCTYPE html>
    <html>
    <head>
        <title>NBA Player Statistics</title>
        <meta charset="utf-8" />
        <link href="https://www.seeklogo.net/wp-content/uploads/2014/09/NBA-logo.png" type="image/png" rel="shortcut icon" />

        <!-- Link to your CSS file that you should edit -->
        <link href="/index.css" type="text/css" rel="stylesheet" />
        <link href="/data-table.css" type="text/css" rel="stylesheet" />
    </head>

    <body>
        <div id="banner">
            <a href="index.php"><img id="banner-image" src="http://www.theinsidewordonline.com/wp-content/uploads/2016/11/17TIWNBAlogo.png" alt="nba logo" /></a>
        </div>

    <?php
}

# Function prints common HTML between pages, specifically the bottom portion of the page after the result
function bottom() {
    ?>

    </div>
        <div id="footer">
            <p>Eric Kha, Copyright 2016</p>
        </div>
    </body>
    </html>

    <?php
}

function search() {
    ?>

    <!-- form to search for every movie by a given actor -->
    <div id="search">
        <form action="search.php" method="get">
            <input name="name" type="text" size="50" placeholder="Enter Player's Name" autofocus="autofocus" />
            <input type="submit" value="Search!" />
        </form>
    </div>

    <?php
}

function printResults($results) {
    header("Content-type: text/html");
//    print_r($results);
//    print($results["games_played"])
?>
        <table class="table-fill">
            <tr>
                <th>GP</th><th>MIN</th><th>FGM</th><th>FGA</th><th>FG%</th><th>3PM</th><th>3PA</th><th>3P%</th>
                <th>FTM</th><th>FTA</th><th>FT%</th><th>OREB</th><th>DREB</th><th>REB</th><th>AST</th><th>TOV</th><th>STL</th>
                <th>BLK</th><th>PF</th><th>PPG</th>
            </tr>
            <tbody>
                <td><?= $results["games_played"] ?></td>
                <td><?= $results["avg_min"] ?></td>
                <td><?= $results["fg_made"] ?></td>
                <td><?= $results["fg_attempted"] ?></td>
                <td><?= $results["fg_pct"] ?></td>
                <td><?= $results["3pt_made"] ?></td>
                <td><?= $results["3pt_attempted"] ?></td>
                <td><?= $results["3pt_pct"] ?></td>
                <td><?= $results["ft_made"] ?></td>
                <td><?= $results["ft_attempted"] ?></td>
                <td><?= $results["ft_pct"] ?></td>
                <td><?= $results["rebounds_off"] ?></td>
                <td><?= $results["rebounds_def"] ?></td>
                <td><?= $results["rebounds_tot"] ?></td>
                <td><?= $results["asst"] ?></td>
                <td><?= $results["turnovers"] ?></td>
                <td><?= $results["steals"] ?></td>
                <td><?= $results["blocks"] ?></td>
                <td><?= $results["pf"] ?></td>
                <td><?= $results["points_per_game"] ?></td>
            </tbody>
        </table>
<?php
}

function printNames($player) {
    header("Content-type: text/html");
    $playerName = preg_replace('/\s+/', '+', $player["name"]);
    $playerName = preg_replace('/%C2%A0/', '', $playerName);
    ?>

    <tr>
        <td id="option2"><a href='search.php?name=<?=htmlspecialchars($playerName)?>'><?=  $player["name"] ?></a></td>
        <td id="option2"><?=  $player["team"] ?></td>
    </tr>


    <?php
}
?>