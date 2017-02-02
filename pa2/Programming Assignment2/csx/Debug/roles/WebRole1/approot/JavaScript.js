(function () {

    window.onload = function () {
        document.getElementById("search").onkeyup = getTitles;
    };

    //function loadWiki() {
    //    $.ajax({
    //        type: "POST",
    //        url: "getQuerySuggestions1.asmx/downloadWiki",
    //        data: "{}",
    //        contentType: "application/json; charset=utf-8",
    //        dataType: "json",
    //        success: alert("loaded wiki")
    //    });
    //}

    function getTitles() {
        var input = document.getElementById("search").value.trim();
        if (input != "") {
            $.ajax({
                type: "POST",
                url: "WebService1.asmx/searchFn",
                data: JSON.stringify({ input: $("#search").val().trim() }),
                contentType: "application/json",
                dataType: 'json',
                success: function (response) {
                    console.log(response);
                    var resultDiv = document.getElementById("results");
                    var table = document.createElement("table");
                    resultDiv.innerHTML = "";
                    if (response.d != null) {
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
})();