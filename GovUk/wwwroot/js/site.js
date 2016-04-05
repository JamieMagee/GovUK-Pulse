$(document).ready(function() {


    var names = {
        https: {
            "-1": "No",
            0: "No",
            1: "Yes, with certificate issues", // (with certificate chain issues)
            2: "Yes"
        },

        https_forced: {
            0: "", // N/A (no HTTPS)
            1: "No", // Present, not default
            2: "Yes", // Defaults eventually to HTTPS
            3: "Yes" // Defaults eventually + redirects immediately
        },

        hsts: {
            "-1": "", // N/A
            0: "No", // No
            1: "Yes", // HSTS on only that domain
            2: "Yes", // HSTS on subdomains
            3: "Yes, and preload-ready", // HSTS on subdomains + preload flag
            4: "Yes, and preloaded" // In the HSTS preload list
        },

        grade: {
            "-1": "",
            0: "M",
            1: "F",
            2: "T",
            3: "C",
            4: "B",
            5: "A-",
            6: "A",
            7: "A+"
        }
    };

    var monthNames = [
        "January", "February", "March",
        "April", "May", "June", "July",
        "August", "September", "October",
        "November", "December"
    ];

    var display = function(set) {
        return function(data, type) {
            if (type === "sort")
                return data;
            else
                return set[data.toString()];
        };
    };

    var linkDomain = function(data, type, row) {
        if (type === "sort")
            return data;
        else
            return "" +
                "<a href=\"" + row["Canonical"] + "\" target=\"blank\">" +
                data +
                "</a>";
    };
    var labsUrlFor = function(domain) {
        return "https://www.ssllabs.com/ssltest/analyze.html?d=" + domain;
    };

    var linkGrade = function(data, type, row) {
        if (type === "display" || type === "filter") {
            var grade = display(names.grade)(data, type);
            if (grade === "")
                return "";
            else
                return "<a href=\"" + labsUrlFor(row["Domain"]) + "\" target=\"blank\">" +
                    grade + "</a>";
        }
        return data;
    };
    //https://govuk.blob.core.windows.net/scans/latest.json

    $("table").DataTable({
        ajax: function(data, callback, settings) {
            settings.sAjaxDataProp = "";
            $.getJSON(
                "https://govuk.blob.core.windows.net/scans/latest.json"
            ).done(function(data, textStatus, request) {
                $(".last-modified").text(parseDate(request.getResponseHeader("Last-Modified")));
                callback(
                    data
                );
            });
        },
        columns: [
            {
                data: "Domain",
                render: linkDomain
            },
            {
                data: "https",
                render: display(names.https)
            },
            {
                data: "grade",
                render: linkGrade
            }
        ],
        initComplete: renderChart,
        responsive: true,
        autoWidth: true
    });


    function renderChart() {
        var total = $("table").DataTable().column(2).data().count();
        var http = ($("table").DataTable().column(2).data().filter(function(value) {
            return value === -1 ? true : false;
        }).count() / total * 100).toPrecision(2);
        var https = (100 - http).toPrecision(2);

        var data = [
            { "status": "inactive", "value": http },
            { "status": "active", "value": https }
        ];

        var chart = d3.select(".https_chart");
        var elem = chart[0][0];
        var width = chart.attr("data-width");
        if (width == null)
            width = getComputedStyle(elem.parentElement).width;
        width = parseInt(width);
        var height = width * 1.2;
        var radius = Math.min(width, height) / 2;
        var color = d3.scale.ordinal()
            .range(["#7ED321", "#FFFFFF"]);
        var arc = d3.svg.arc()
            .outerRadius(radius)
            .innerRadius(radius - 40);
        var pie = d3.layout.pie()
            .value(function(d) {
                return d.value;
            })
            .sort(null);
        chart = chart
            .append("svg")
            .attr("width", width)
            .attr("height", height)
            .append("g")
            .attr("transform", "translate(" + (width / 2) + "," + (height / 2) + ")");
        var g = chart.selectAll(".arc")
            .data(pie(data))
            .enter().append("g")
            .attr("class", "arc");
        g.append("path")
            .style("fill", function(d) {
                return color(d.data.status);
            })
            .transition().delay(function(d, i) {
                return i * 400;
            }).duration(400)
            .attrTween("d", function(d) {
                var i = d3.interpolate(d.startAngle + 0.1, d.endAngle);
                return function(t) {
                    d.endAngle = i(t);
                    return arc(d);
                };
            });
        g.append("text")
            .attr("text-anchor", "middle")
            .attr("class", "total-value")
            .attr("dy", "0.2em")
            .attr("fill", "white")
            .text(function(d) {
                return "" + data[0].value + "%";
            });
        g.append("text")
            .attr("text-anchor", "middle")
            .attr("class", "total-desc")
            .attr("dy", "2.5em")
            .attr("fill", "white")
            .text(function(d) {
                return "USE HTTPS";
            });
    }

    function parseDate(dateString) {
        var date = new Date(dateString);
        var month = date.getMonth();
        var day = date.getDate();

        return day + " " + monthNames[month];
    }

})