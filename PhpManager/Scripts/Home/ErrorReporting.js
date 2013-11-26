
$(document).ready(function() {
    $("input[type=radio]").each(function () {
        this.onclick = function () {

            // Get the desired mode
            var mode = $("input:radio[name=settingMode]:checked").val();
            var selectedOption = $("input:radio[name=settingMode]:checked");
            
            // Set the mode settings
            $.ajax(
                {
                    url: setModeUrl,
                    data: JSON.stringify({
                        "mode": mode
                    }),
                    type: "POST",
                    contentType: "application/json; charset=utf-8",
                    statusCode: {
                        404: function () {
                            alert("404: /Home/SaveSettingValue page not found");
                        }
                    },
                    error: function (results) {
                        alert("Some error occurred");
                    },
                    success: function (results) {
                        if (!results.Success) {
                            alert("Couldn't save state: " + results.ErrorMessage);
                            selectedOption.prop('checked', false);
                        }
                    },
                }
            );
        };
    });
});