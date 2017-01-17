<?php
# Includes PHP file to use its functions
include_once("/srv/www/htdocs/common.php");
header("Content Type: text/html");
top();
?>
    <div id="main-page">
        <h1>No Player Found</h1>
        <p>Sorry, <?= htmlspecialchars($_GET['name']) ?> was not found! Try searching for a different player</p>

<?php
search();
bottom();
?>