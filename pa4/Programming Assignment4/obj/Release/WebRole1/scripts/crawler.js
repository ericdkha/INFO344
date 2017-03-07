// Function that makes an ajax request to the StartCrawling method of the Webrole that puts the crawler at a state of either loading or crawling
function StartCrawler() {
    $.ajax({
        type: "POST",
        url: "wiki.asmx/StartCrawling",
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
        }
    });
}

// Function that makes an ajax request to the StopCrawling method of the Webrole that puts the crawler at an idle state
function StopCrawler() {
    $.ajax({
        type: "POST",
        url: "wiki.asmx/StopCrawling",
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
        }
    });
}

// Function that makes an ajax request to the ClearIndex method of the Webrole that clears everything loaded thus far and puts the crawler
// in an idle state
function ClearAll() {
    $.ajax({
        type: "POST",
        url: "wiki.asmx/ClearIndex",
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
            $("#resultSet").empty();
            $("#url-crawled").html("0");
            $("#size-of-queue").html("0");
            $("#size-of-Indexed").html("0");
            $("#ram-available").empty();
            $("#resultSetP").empty();
            $("s").empty();
            $("#cpu-utilization").html("0%");
            $("ol.urls").empty();
            $("ul.urls").empty();
            alert("We're gonna be hard at work clearing out everything, please be patient before clicking on another button (Approx. Wait Time is 30 seconds) :)");
        }
    });
}

//// Function that makes an ajax request to the GetPageTitle method of the Webrole that searches existing entries for the page title with the given
//// URL and displays it for the users
//function GetPageTitle() {
//    $.ajax({
//        type: "POST",
//        url: "wiki.asmx/GetPageTitle",
//        data: JSON.stringify({ url: $("#s").val().trim() }),
//        contentType: "application/json; charset=utf-8",
//        dataType: 'json',
//        success: function (data) {
//            $("#resultSetP").html(JSON.parse(data.d));
//        }
//    });
//}

// Function that makes an ajax request to the GetDashboard method of the Webrole that collects the currents information about the crawler such as:
// # of urls crawled, the size of the queue/pipline, and the num of indexed urls and displays it for the users
function GetDashboard() {
    $.ajax({
        type: "POST",
        url: "wiki.asmx/GetDashboard",
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
            $("#url-crawled").html(JSON.parse(data.d[0]));
            $("#size-of-queue").html(JSON.parse(data.d[1]));
            $("#size-of-Indexed").html(JSON.parse(data.d[2]));
        }
    });
}

function GetTrieStats() {
    $.ajax({
        type: "POST",
        url: "wiki.asmx/GetTrieStats",
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
            $("#size-of-trie").html(JSON.parse(data.d[0]));
            $("#last-of-trie").html(data.d[1]);
        }
    });
}

// Function that makes an ajax request to the GetRam method of the webrole that gets information about the current available mb for the crawler
// and displays it for the users
function GetRam() {
    $.ajax({
        type: "POST",
        url: "wiki.asmx/GetRam",
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
            $("#ram-available").html(JSON.parse(data.d));
        }
    });
}

// Function that makes an ajax request to the GetCpu method of the webrole that gets information about the current cpu utilization of the crawler
// and displays it for the users
function GetCpu() {
    $.ajax({
        type: "POST",
        url: "wiki.asmx/GetCpu",
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
            $("#cpu-utilization").html(JSON.parse(data.d));
        }
    });
}

// Function that makes an ajax request to the lastTen method of the webrole that gets infomration about the last ten urls that have been indexed 
// and displays it for the users
function GetLastTen() {
    $.ajax({
        type: "POST",
        url: "wiki.asmx/lastTen",
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
            $("ol.urls").empty();
            var urlList = $('ol.urls');
            for (var i = data.d.length - 1; i >= 0; i--) {
                var item = data.d[i];
                var li = $('<li/>')
                    .text(item)
                    .appendTo(urlList);
            }
        }
    });
}

// Function that makes an ajax request to the errors method of the webrole that gets infomration about the errors that have been encountered 
// and displays it for the users
function GetErrors() {
    $.ajax({
        type: "POST",
        url: "wiki.asmx/errors",
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
            $("ul.urls").empty();
            var urlList = $('ul.urls');
            for (var i = data.d.length - 1; i >= 0; i--) {
                var item = data.d[i];
                var li = $('<li/>')
                    .text(item)
                    .appendTo(urlList);
            }
        }
    });
}

// Function that makes an ajax request to the getCommand method of the webrole that gets infomration about the the current state of the crawler
// and displays it for the users
function GetState() {
    $.ajax({
        type: "POST",
        url: "wiki.asmx/getCommand",
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        success: function (data) {
            if (JSON.parse(data.d) === "idle") {
                $("#crawler-state").html("Idle");
            } else if (JSON.parse(data.d) === "loading") {
                $("#crawler-state").html("Loading");
            } else {
                $("#crawler-state").html("Crawling");
            }
        }
    });
}


// Function that calls all the functions that provides the information to the user
function Refresh() {
    GetDashboard();
    GetTrieStats();
    GetCpu();
    GetRam();
    GetLastTen();
    GetErrors();
    GetState();
}

// Function to call Refresh so that the dashboard gets updated with new information for the users
function loop() {
    Refresh();
    setTimeout("loop()", 3000);
}
setTimeout(Refresh(), 1000);
