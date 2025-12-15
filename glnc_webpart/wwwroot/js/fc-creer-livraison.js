$(document).ready(function () {

    var newEvent = null;
    var idNewEvent = null;
    var eventMinimumMinutes = 60;

    function resultCreationLivraison(result) {
        switch (result) {
            case "0Driver":
                $('#modalErreur').modal('show');
                $("#modalErreurLabel").text("Tous les chauffeurs ont une livraison prévue à ce moment-là.\n"
                    + "Trouvez un autre créneau pour la livraison.");
                break;
            case "0Camion":
                $('#modalErreur').modal('show');
                $("#modalErreurLabel").text("Tous les camions ont une livraison planifiée à ce moment.\n"
                    + "Trouvez un autre créneau pour la livraison.");
                break;
            case "erreur":
                $('#modalErreur').modal('show');
                $("#modalErreurLabel").text("Erreur inconnue. Impossible d'enregistrer la livraison");
                break;
            default:
                idNewEvent = result;
                newEvent = null;
                bon = null;
                $('#calendar').fullCalendar('refetchEvents');
                $('#modalOuiNon').modal('show');
                $("#modalOuiNonQuestion").text("Livraison enregistrée. Souhaitez-vous compléter celle-ci ?");
                break;
        }
    }

    $('#btnCreerLivraison').on('click', function () {
        CreerLivraison()
    });

    function CreerLivraison() {
        var idDriver = $("#ddlDriver")[0].value;
        if (!idDriver) {
            $('#modalErreur').modal('show');
            $("#modalErreurLabel").text("Veuillez sélectionner un conducteur");
            return;
        }
        var idCamion = $("#ddlCamion")[0].value;
        if (!idCamion) {
            $('#modalErreur').modal('show');
            $("#modalErreurLabel").text("Veuillez sélectionner un camion");
            return;
        }
        $.ajax({
            url: location.origin + '/api/Plannif/DemandeCreationLivraison/'
                + moment(newEvent.start).unix() + "/"
                + moment(newEvent.end).unix(),
            type: "GET",
            contentType: "application/json; charset=utf-8",
            dataType: "JSON",
            success: function (result) {
                switch (result) {
                    case "Ok":
                        if (idDriver === "") idDriver = "0";
                        if (idCamion === "") idCamion = "0";

                        $.ajax({
                            url: location.origin + "/api/Plannif/CreationLivraison/"
                                + moment(newEvent.start).unix() + "/"
                                + moment(newEvent.end).unix()
                                + "/" + idDriver + "/" + idCamion,
                            type: "GET",
                            contentType: "application/json; charset=utf-8",
                            dataType: "JSON",
                            success: function (result) {
                                resultCreationLivraison(result);
                            },
                            error: function (xhr, status, error) {
                                console.error("Error creating delivery:", error);
                                $('#modalErreur').modal('show');
                                $("#modalErreurLabel").text("Error creating delivery. Please try again.");
                            }
                        });
                        break;
                    // case "0Camion":
                    //     $('#modalErreur').modal('show');
                    //     $("#modalErreurLabel").text("Livraison impossible dans cette zone à ce moment car le créneau est complet.");
                    //     bon = null;
                    //     break;
                }
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                alert("Erreur lors de la demande de création. Vérifier la syntaxe du bon.");
            },
            statusCode: {
                404: function () {
                    alert("Une valeur potentiellement dangereuse a été détectée à partir du client.");
                }
            }
        });
    }


    $('#btnModalOui').on('click', function () {
        console.log(idNewEvent);
        window.location.assign('/Livraison/Edit/' + idNewEvent);
    });
    $("#btnModalCancel").on("click", function () {
        $.ajax({
            url: location.origin + '/api/Plannif/DeleteLivraison/'
                + idNewEvent,
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "JSON",
            success: function (result) {
                if (result == "Ok") {
                    window.location.assign('/Livraison/Schedule/');
                }
            }
        });
    })

    $('#calendar').fullCalendar({
        schedulerLicenseKey: '0176303871-fcs-1711703315',
        customButtons: {
            enregistrer: {
                text: 'Enregistrer',
                click: function () {
                    if (newEvent === null) {
                        $('#modalErreur').modal('show');
                        $("#modalErreurLabel").text("Veuillez choisir la date et l'heure de livraison.");
                        return;
                    }
                    CreerLivraison()
                }
            }
        },
        header:
        {
            left: 'prev,next today',
            center: 'title',
            right: 'month,agendaWeek,agendaDay,listWeek,enregistrer'
        },
        buttonText: {
            today: 'Aujourd\'hui',
            month: 'Mois',
            agendaWeek: 'Semaine',
            agendaDay: 'Jour',
            listWeek: 'Liste'
        },

        //////////////////////OPTIONS//////////////////////////////

        height: 800,
        width: '100%',
        timeZone: 'Pacific/Noumea',
        locale: 'fr',
        firstDay: 1,
        plugins: ['interaction', 'timeGrid', 'dayGrid', 'list'],
        selectable: true,
        editable: true,
        eventDurationEditable: true,
        eventStartEditable: true,
        defaultView: 'agendaWeek',
        businessHours: true,
        hiddenDays: [7],
        minTime: "05:00:00",
        maxTime: "19:00:00",
        allDaySlot: false,
        views: {
            month: {
                titleFormat: 'MMMM YYYY',
                dayNames: ['Dim', 'Lun', 'Mar', 'Mer', 'Jeu', 'Ven', 'Sam']
            },
            agendaWeek: {
                titleFormat: 'D MMM YYYY',
                dayNames: ['Dim', 'Lun', 'Mar', 'Mer', 'Jeu', 'Ven', 'Sam']
            },
            agendaDay: {
                titleFormat: 'dddd D MMMM YYYY',
                dayNames: ['Dim', 'Lun', 'Mar', 'Mer', 'Jeu', 'Ven', 'Sam']
            },
            listWeek: {
                titleFormat: 'D MMM YYYY',
                listDayFormat: 'dddd D MMMM',
                listDayAltFormat: 'dddd D MMMM YYYY'
            }
        },

        //////////////////////EVENTS//////////////////////////////

        select: function (start, end, jsEvent, view) {

            end._i[3] = start._i[3] + 1;
            end._i[4] = start._i[4];

            end._d = new Date(start._d);
            end._d.setMinutes(start._d.getMinutes() + 60);

            var duration = moment.duration(end.diff(start));
            var minutes = duration.asMinutes();
            if (minutes >= eventMinimumMinutes) {
                $('#calendar').fullCalendar('refetchEvents');
                newEvent = new Object();
                newEvent.editable = true;
                newEvent.title = minutes + ' minute(s)';
                newEvent.start = start;
                newEvent.end = end;

                newEvent.color = "#20bb20";
                newEvent.id = "nvleLivraison";
                $('#calendar').fullCalendar('renderEvent', newEvent);
            }
        },

        selectOverlap: function (event) {
            return event.rendering === 'background';
        },

        events: function (start, end, timezone, callback) {
            var idCamion = $("#ddlCamion")[0].value;
            var idDriver = $("#ddlDriver")[0].value;

            if (idCamion === "") idCamion = "0";
            if (idDriver === "") idDriver = "0";

            // Normalize to ISO week (Monday) and fetch for all visible weeks
            var weekStart = moment(start).startOf('isoWeek');
            var weekEnd = moment(end).endOf('isoWeek');

            var requests = [];
            var current = weekStart.clone();
            while (current.isBefore(weekEnd)) {
                requests.push($.ajax({
                    url: location.origin + '/api/Plannif/LivraisonZonageSemaine/' + current.format('YYYY-MM-DD') + "/" + idCamion + "/" + idDriver,
                    type: "GET",
                    contentType: "application/json; charset=utf-8",
                    dataType: "JSON"
                }));
                current.add(1, 'week');
            }

            $.when.apply($, requests).done(function () {
                var allResults = [];
                if (requests.length === 1) {
                    allResults = arguments[0];
                } else {
                    for (var i = 0; i < arguments.length; i++) {
                        Array.prototype.push.apply(allResults, arguments[i][0]);
                    }
                }

                var events = [];
                var seen = {};
                $.each(allResults, function (i, data) {
                    var imgStatut;
                    switch (data.statut) {
                        case 1: imgStatut = "cancel.150x150.png"; break;
                        case 2: imgStatut = "accept.150x150.png"; break;
                        case 3: imgStatut = "deliver.150x150.png"; break;
                        default: imgStatut = "refuse.150x150.png"; break;
                    }

                    if (seen[data.id]) return; // de-duplicate across weeks
                    seen[data.id] = true;

                    events.push({
                        idLivraison: data.id,
                        id: data.title,
                        backgroundColor: data.statut === 3 ? "#a0a0a0" : data.color,
                        editable: (data.statut !== 3),
                        title: data.client || "",
                        description: data.desc,
                        camion: data.camion,
                        start: moment(data.start).format('YYYY-MM-DD HH:mm'),
                        end: moment(data.end).format('YYYY-MM-DD HH:mm'),
                        backgroundColor: data.color,
                        borderColor: "#000000",
                        tooltip: data.description,
                        lock: true,
                        rendering: data.typevt === 0 ? '' : 'background',
                        imageurl: "/img/statut_livraison/" + imgStatut,
                        livraison: true
                    });
                });
                callback(events);
            }).fail(function () {
                callback([]);
            });
        },

        eventDrop: function (event, delta, revertFunc) {
            // Only update if it's not a new unsaved event
            if (event.id !== "nvleLivraison") {
                $.ajax({
                    url: location.origin + '/api/Plannif/UpdateLivraison/' + event.id
                        + '/' + moment(event.start).unix()
                        + '/' + moment(event.end).unix(),
                    type: "POST",
                    success: function (result) {
                        if (result !== "Ok") {
                            alert("Erreur lors de la mise à jour !");
                            revertFunc(); // Undo the move if error
                        }
                    },
                    error: function () {
                        alert("Erreur lors de la mise à jour !");
                        revertFunc(); // Undo the move if error
                    }
                });
            } else {
                // For new events, just update the local newEvent object
                newEvent.start = event.start;
                newEvent.end = event.end;
            }
        },
        eventResize: function (event, delta, revertFunc) {
            var minutes = moment.duration(event.end.diff(event.start)).asMinutes();

            // Allow resize for existing events (not new unsaved events)
            if (event.id !== "nvleLivraison") {
                if (minutes >= eventMinimumMinutes) {
                    $.ajax({
                        url: location.origin + '/api/Plannif/UpdateLivraison/' + event.id
                            + '/' + moment(event.start).unix()
                            + '/' + moment(event.end).unix(),
                        type: "POST",
                        success: function (result) {
                            if (result !== "Ok") {
                                alert("Erreur lors de la mise à jour !");
                                revertFunc();
                            }
                        },
                        error: function () {
                            alert("Erreur lors de la mise à jour !");
                            revertFunc();
                        }
                    });
                } else {
                    alert("La durée minimale est de " + eventMinimumMinutes + " minutes.");
                    revertFunc();
                }
            } else {
                // For new events, update local object
                if (minutes >= eventMinimumMinutes) {
                    newEvent.title = minutes + ' minute(s)';
                    newEvent.start = event.start;
                    newEvent.end = event.end;
                } else {
                    alert("La durée minimale est de " + eventMinimumMinutes + " minutes.");
                    revertFunc();
                }
            }
        },

        /////////////////////////////////////////////////////

        eventRender: function (eventObj, el) {
            // Add click handler for editing events
            el.bind('click', function (e) {
                // Don't trigger if clicking on delete button
                if ($(e.target).hasClass('delete-event')) {
                    return;
                }

                if (eventObj.id !== "nvleLivraison" && !$.isNumeric(eventObj.id)) {
                    window.location.assign('/Livraison/Edit/' + eventObj.id);
                }
            });

            // Add visual feedback for draggable events
            if (eventObj.editable) {
                el.addClass('fc-event-draggable');
                el.attr('title', 'Cliquez pour modifier, glissez pour déplacer');
            }

            // fix pour popover du fullcalendar qui reste affiché
            $('.popover.fade.top').remove();

            if ($.isNumeric(eventObj.camion)) $(el).css('width', 100 / eventObj.camion + '%');

            $(el).popover({
                //title: eventObj.title,
                content: eventObj.description,
                trigger: 'hover',
                placement: 'top',
                container: 'body'
            });

            // Add delete button for editable events (not for new unsaved events)
            if (eventObj.editable && eventObj.id !== "nvleLivraison") {
                var deleteBtn = $("<span class='delete-event' style='cursor:pointer; color:red; margin-left:5px; font-weight:bold; font-size: 12px;' title='Supprimer cette livraison'>&#10006;</span>");
                $(el).find('.fc-title').append(deleteBtn);

                deleteBtn.on('click', function (e) {
                    e.stopPropagation(); // Prevent triggering other events
                    if (confirm("Voulez-vous supprimer cette livraison ?")) {
                        // Remove from calendar UI
                        $('#calendar').fullCalendar('removeEvents', eventObj.id);

                        // Send AJAX to backend to delete from DB
                        $.ajax({
                            url: location.origin + '/api/Plannif/DeleteLivraison/' + eventObj.id,
                            type: "POST",
                            success: function (result) {
                                if (result !== "Ok") {
                                    alert("Erreur lors de la suppression !");
                                    // Refresh calendar to show the event again if deletion failed
                                    $('#calendar').fullCalendar('refetchEvents');
                                }
                            },
                            error: function () {
                                alert("Erreur lors de la suppression !");
                                // Refresh calendar to show the event again if deletion failed
                                $('#calendar').fullCalendar('refetchEvents');
                            }
                        });
                    }
                });
            }

            if (!eventObj.lock && eventObj !== null) {

                el.find(".fc-time").prepend("<img class='supprime_event_fullcal'></img>");
                el.find(".supprime_event_fullcal").on('click', function () {
                    if ($('#calendar').children().length > 0) {
                        $('#calendar').fullCalendar('removeEvents', eventObj._id);
                        newEvent = null;
                    };
                });
            }

            if (eventObj.imageurl) {
                el.find("div.fc-time").prepend("<img src='" + eventObj.imageurl + "' width='16' height='16' style='margin-right: 3px;'>");
            }

            // Add resize handles for events
            if (eventObj.editable) {
                el.addClass('fc-event-resizable');
            }
        }
    });
});

$('#ddlCamion').change(function () {
    if ($('#calendar').children().length > 0) $('#calendar').fullCalendar('refetchEvents');
}).change();

$('#ddlDriver').change(function () {
    if ($('#calendar').children().length > 0) $('#calendar').fullCalendar('refetchEvents');
}).change();