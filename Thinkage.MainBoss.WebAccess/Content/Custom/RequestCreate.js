/*
    The autocomplete javascript pattern enabler for the UnitLocationID input field on the RequestCreate.
    This is a System.Text.RegularExpression applied to all the Location.Code values
    returned for all units that match the term typed in by the user.
*/
var RequestCreateUnitCodePattern = function (requestorName, requestorEmail) {
    return null; /* null means no regular expression matching */
}
/*
    Change to false if you do NOT want Autocomplete of the Units table codes to be used.
*/
var RequestCreateUnitCodeAutocompleteEnabled = function () {
    return true;
}