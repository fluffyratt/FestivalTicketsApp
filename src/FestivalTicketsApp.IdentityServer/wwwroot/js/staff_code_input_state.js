$(document).ready(() => {
    let roleSelect = $('#roleSelect');
    changeRoleSelectVisibility(roleSelect)
    roleSelect.change(() => changeRoleSelectVisibility(roleSelect))
})

const changeRoleSelectVisibility = (roleSelect) => {
    let isClientRoleSelected = roleSelect.val() === "client";
    let staffCodeInputDiv = $('#staffCodeInputDiv');
    staffCodeInputDiv.prop("hidden", isClientRoleSelected);
}