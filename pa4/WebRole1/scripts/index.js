(function () {

    //window.onload = function () {
    //    document.getElementById("search").onkeyup = getTitles;
    //};

    $(document).ready(function () {
        $("#playerData").hide();
        $("#cacheButton").on("click", function () {
            ClearCache();
        });

        $("#search").on("keyup", function () {
            GetTitles();
            QueryAws();
            Search();
        });

        $("#search-button").click(function () {
            GetTitles();
            QueryAws();
            Search();
        });
    });

    function GetTitles() {
        var input = document.getElementById("search").value.trim();
        if (input !== "") {
            $.ajax({
                type: "POST",
                url: "wiki.asmx/searchFn",
                data: JSON.stringify({ input: $("#search").val().trim() }),
                contentType: "application/json",
                dataType: 'json',
                success: function (response) {
                    var resultDiv = document.getElementById("results");
                    var table = document.createElement("table");
                    resultDiv.innerHTML = "";
                    if (response.d !== null) {
                        var tableBodyEl = document.createElement("tbody");
                        for (var i = 0; i < response.d.length; i++) {
                            var suggestionsEl = document.createElement("tr");
                            var titleEL = document.createElement("p");
                            titleEL.innerHTML = response.d[i];
                            suggestionsEl.appendChild(titleEL);
                            tableBodyEl.appendChild(suggestionsEl);
                           
                        }
                        table.appendChild(tableBodyEl);
                        resultDiv.appendChild(table);

                    }
                }
            });
        } else {
            document.getElementById("results").innerHTML = "";
        }
    }

    function QueryAws() {
        var input = $("#search").val().trim();
        if (input !== "") {
            $.ajax({
                crossDomain: true,
                contentType: "application/json; charset=utf-8",
                url: "http://ec2-35-161-42-3.us-west-2.compute.amazonaws.com/jsonp.php",
                data: { name: input },
                dataType: "jsonp",
                error: function (data) {
                    $("#playerData").hide();
                    $("#playerName").html("");
                    $("#playerStats").html("");
                },
                success: function (data) {
                    $("#playerName").html("");
                    $("#playerStats").html("");
                    var name = "";
                    var stats = "";
                    name = data["player"].name;
                    var team = data["player"].team;
                    var regExp = /\(([^)]+)\)/;
                    team = regExp.exec(team);
                    var picture = "http://i.cdn.turner.com/nba/nba/assets/logos/teams/primary/web/" + team[1] + ".svg";
                    //picture = " <embed src=" + picture + " width=" + 100 + " height=" + 100 + ">";
                    document.getElementById("playerName").style.backgroundImage = "url(" + picture + ")";
                    //$("#playerName").css("background-image", "url(" + picture + ")");
                    console.log(name);
                    stats += "Team: " + data["player"].team + "<br />" +
                                "Games Played: " + data["player"].gp + "<br />" +
                                "Avg Points Per Game: " + data["player"].points_per_game + "<br />" +
                                "Field Goal %: " + data["player"].fg_pct + "<br />" +
                                "Free Throw %: " + data["player"].ft_pct + "<br />" +
                                "Total Rebounds: " + data["player"].rebounds_tot + "<br />" +
                                "Assists: " + data["player"].asst + "<br />" +
                                "Turnovers: " + data["player"].turnovers + "<br />" +
                                "Steals: " + data["player"].steals + "<br />" +
                                "Blocks: " + data["player"].blocks + "<br />";

                    if (name !== "") {
                        $("#playerName").html(name);
                        $("#playerStats").html(stats);
                        $("#playerData").show();
                    }
                }
            });
        } else {
            $("#playerData").hide();
            $("#playerName").html("");
            $("#playerStats").html("");
        }
    }

    function Search() {
        var input = $("#search").val().toLowerCase().trim();
        //if (input[0] !== "" && input[0] !== " ") {
        var result = "";
        var uniqueResults = [];
        //for (var j = 0; j < input.length; j++) {
        $.ajax({
            type: "POST",
            url: "wiki.asmx/GetPageTitle",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify({ input: input }),
            dataType: "json",
            success: function (data) {
                if (data.d !== null && data.d[0] !== null && data.d.length >= 1) {
                    jQuery.each(data.d, function (i, val) {
                        if ($.inArray(data.d[i][0], uniqueResults) === -1) {
                            result += "<div><a href=" + data.d[i][1] + "><h3>" + data.d[i][0] + "</h3></a>";
                            result += "<p class= resultUrl>" + data.d[i][1] + "</p>";
                            result += "<p class= resultTime>" + data.d[i][2] + "</p><br /><br /></div>";
                            uniqueResults.push(data.d[i][0]);
                        }
                    });
                }
                $("#searchResults").html(result);
            }
        });
            //}
    }
        //else {
        //    $("#searchResults").html("");
        //}

    //function showPlayer(data) {

    //}

    function ClearCache() {
        $.ajax({
            type: "POST",
            url: "wiki.asmx/ClearCache",
            data: JSON.stringify({}),
            contentType: "application/json; charset=utf-8",
            dataType: 'json',
            success: function (data) {
            }
        });
    }
})();