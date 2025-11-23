$(document).ready(() => {
    let eventTypeSelector = $('#NewEventInfo_EventTypeId');
    let previousEventType = eventTypeSelector.val();
    
    let genreSelector = $('#NewEventInfo_GenreId');

    eventTypeSelector.change(() => {
        if (previousEventType !== eventTypeSelector.val())
            fillGenreSelectorWithOptions(eventTypeSelector.val(), genreSelector);
        previousEventType = eventTypeSelector.val();
    });
});

const fillGenreSelectorWithOptions = (eventTypeId, genreSelector) => {
    $.get(`../GenresOptions/${eventTypeId}`, (data) => {
        genreSelector.empty();
        genreSelector.append(data);
    });
};
