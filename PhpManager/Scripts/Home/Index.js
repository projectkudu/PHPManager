
$(document).ready(function () {

    // We don't want the list of all extensions to be visible initialy
    $("#extensions").hide();

    $("#showExtensions").each(function() {
        this.onclick = function() {
            $("#extensions").toggle();
            var oldText = $(this).text();
            if (oldText == 'Hide all') {
                $(this).text("Show all");
            } else {
                $(this).text("Hide all");
            }
            
        };
    });

});