
$(document).ready(function () {
    $("button.save").each(function () {
        this.onclick = function () {

            // Determine setting name and the new value of the setting
            var settingName = this.id.replace('-btn', '');
            var settingValueNode = $("input[id='" + settingName + "-input']");
            var newValue = settingValueNode.val();
            var oldValue = settingValueNode.data("savedvalue");

            if (oldValue == newValue) {
                return;
            }

            // Save the value
            $.ajax(
                {
                    url: saveSettingUrl,
                    data: JSON.stringify({
                        "settingName": settingName,
                        "settingValue": newValue
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
                        if (results.Success) {
                            // We saved the setting successfully
                            $("img[id='" + settingName + "-statusNone']").hide();
                            $("img[id='" + settingName + "-statusSuccess']")
                                .show()
                                .delay(3000)
                                .fadeOut(300);
                            $("td[id='" + settingName + "-errorMsg']").text("");
                            settingValueNode.data("savedvalue", newValue);
                        }
                        else {
                            // Could not save value
                            $("img[id='" + settingName + "-statusNone']").hide();
                            $("img[id='" + settingName + "-statusFail']").show().delay(10000).fadeOut(300);
                            settingValueNode.val(oldValue); // Revert the value in the text box
                            $("td[id='" + settingName + "-errorMsg']")
                                .show()
                                .text(results.Message)
                                .delay(10000)
                                .fadeOut(300);
                        }
                    },
                }
            );
        };
    });
    
    setupNewSettingButton();

    setupRevertAllButton();
    

});

function setupNewSettingButton() {
    $("#newSettingBtn").each(function () {
        this.onclick = function () {
            var newName = $("#newName").val();
            var newValue = $("#newValue").val();
            var section = $("#newSection").val();

            if (section == '') {
                section = "None";
            }

            // Save the value
            $.ajax(
                {
                    url: saveSettingUrl,
                    data: JSON.stringify({
                        "settingName": newName,
                        "settingValue": newValue,
                        "settingSection": section
                    }),
                    type: "POST",
                    contentType: "application/json; charset=utf-8",
                    success: function () {
                        location.reload();
                    }
                });


        };
    });
}

function setupRevertAllButton() {
    $("#revertAllSettings").each(function() {
        this.onclick = function() {
            $.ajax(
                {
                    url: revertAllSettingUrl,
                    type: "POST",
                    success: function () {
                        location.reload();
                    }
                });
        };
    });
}