$().ready(function () {

    $("#validationError").hide();
    $("#submitError").hide();

    $('.cc-number').payment('formatCardNumber');
    $('.cc-exp').payment('formatCardExpiry');
    $('.cc-cvc').payment('formatCardCVC');

    $('#cardDetailsForm').submit(function (event) {

        var $form = $(this);

        $("#cardDetailsForm").validate({
            CardholdersName: {
                required: true
            },

            CardNumber: {
                required: true
            },

            CardExpiryDate: {
                required: true
            },

            CardCCV: {
                required: true
            },
            errorPlacement: function (error, element) {
            },
            invalidHandler: function (event, validator) {
                var errors = validator.numberOfInvalids();
                if (errors) {
                    var message = "<h3>Form error!</h3><p>We're sorry, there was a problem submitting your form. Please check the fields highlighted below to resolve any errors.</p>";
                    $("#validationError").html(message);
                    $("#validationError").show();
                }
                else {
                    $("#validationError").hide();
                }
            },
            submitHandler: function () {
                $(window).spin();
            }
        });
        return false;
    });

});
