<?php

# Includes PHP file to use its functions
include_once("/srv/www/htdocs/common.php");
header("Content Type: text/html");
top();
?>
    <div id="main-page">
        <h1>NBA Players</h1>
        <p>Type in a player's name to view their 2015-16 stats!</p>

<?php
search();
bottom();
?>