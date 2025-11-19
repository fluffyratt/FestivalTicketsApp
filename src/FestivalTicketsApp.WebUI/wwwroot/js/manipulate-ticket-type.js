const $document = $(document);
const $createTicketType = $('.createTicketType');
const $ticketTypesWrapper = $('.ticketTypesWrapper');
const $ticketTypesCount = $('#ticketTypesCount');
const $mappingSelects = $('.mappingSelects');
const maxTicketTypesCount = Number($("#maxTicketTypesCount").val());

let previousTypeName;

const ticketTypeTemplate = (index) => `
    <div id="ticketType-${index}" class="ticketTypeWrapper row mt-3">
        <div class="col-4">
            <label class="form-label" for="NewEventInfo_TicketTypes_${index}__Name">Type name</label>
            <input class="ticketTypeInput form-control" type="text" data-val="true" data-val-required="The Name field is required." 
                id="NewEventInfo_TicketTypes_${index}__Name" name="NewEventInfo.TicketTypes[${index}].Name" />
        </div>
        <div class="col-4">
            <label class="form-label" for="NewEventInfo_TicketTypes_${index}__Price">Price</label>
            <input class="ticketTypePrice form-control" type="text" data-val="true" 
                data-val-number="The field Price must be a number." data-val-required="The Price field is required." 
                id="NewEventInfo_TicketTypes_${index}__Price" name="NewEventInfo.TicketTypes[${index}].Price"  />
        </div>
        <div class="col-1 mt-auto">
            <button id="deleteTicketType-${index}" class="deleteTicketType btn btn-danger">Delete</button>
        </div>
    </div>`;


$document.ready(() => {
    $createTicketType.click(handleCreateTicketType);
    $document.on('click', '.deleteTicketType', handleDeleteTicketType);
    $document.on('focus', '.ticketTypeInput', handleTicketTypeFocus);
    $document.on('blur', '.ticketTypeInput', handleTicketTypeBlur);
    $('.ticketTypePrice').maskMoney({thousands: "", decimal:","});
});

const handleCreateTicketType = (e) => {
    e.preventDefault();
    let ticketTypesCount = Number($ticketTypesCount.val());

    if (ticketTypesCount >= maxTicketTypesCount) {
        return;
    }

    $ticketTypesWrapper.append(ticketTypeTemplate(ticketTypesCount));
    $ticketTypesCount.val(++ticketTypesCount);

    if (ticketTypesCount >= maxTicketTypesCount) {
        $createTicketType.prop('disabled', true);
    }
    $('.ticketTypePrice').maskMoney({thousands: "", decimal:","});
};

const handleDeleteTicketType = (e) => {
    e.preventDefault();
    const deleteButtonIndex = $(e.target).attr('id').match(/\d+/g)[0];
    const $ticketTypeWrapperToDelete = $ticketTypesWrapper.find(`#ticketType-${deleteButtonIndex}`);
    const ticketTypeOptionToDelete = $ticketTypeWrapperToDelete.find('.ticketTypeInput').val();

    $mappingSelects.each((index, select) => {
        const $select = $(select);
        $select.find(`option[value="${ticketTypeOptionToDelete}"]`).first().remove();
    });

    $($ticketTypeWrapperToDelete).remove();

    let ticketTypesCount = Number($ticketTypesCount.val());
    $ticketTypesCount.val(--ticketTypesCount);

    let ticketTypesIndex = 0;
    $ticketTypesWrapper.children('.ticketTypeWrapper').each((index, childElement) => {
        let $childElement = $(childElement);

        let attributeNewValue = $childElement.attr('id').replace(/\d+/g, ticketTypesIndex);
        $childElement.attr('id', attributeNewValue);

        changeTicketTypeWrapperChildrenId($childElement, "label", ticketTypesIndex, "for");
        changeTicketTypeWrapperChildrenId($childElement, "input", ticketTypesIndex, "name", "id");
        changeTicketTypeWrapperChildrenId($childElement, "button", ticketTypesIndex, "id");

        ticketTypesIndex++;
    });

    $createTicketType.prop('disabled', false);
};

const handleTicketTypeFocus = (e) => {
    previousTypeName = $(e.target).val();
};

const handleTicketTypeBlur = (e) => {
    let newTypeName = $(e.target).val();

    if (newTypeName === previousTypeName) {
        return;
    }

    $mappingSelects.each((index, select) => {
        const $select = $(select);

        if (!newTypeName) {
            $select.find(`option[value="${previousTypeName}"]`).first().remove();
            return;
        }
        const previousOption = $select.find(`option[value="${previousTypeName}"]`).first();
        if (previousOption.length > 0) {
            previousOption.text(newTypeName).val(newTypeName);
        } else {
            $select.append($('<option>', {
                value: newTypeName,
                text: newTypeName
            }));
        }
    });
};

const changeTicketTypeWrapperChildrenId = (fatherSelector, tag, newIndex, ...attributes) => {
    fatherSelector.find(tag).each((index, element) => {
        let $element = $(element);
        for (let attribute of attributes) {
            let attributeNewValue = $element.attr(attribute).replace(/\d+/g, newIndex);
            $element.attr(attribute, attributeNewValue);
        }
    });
};
