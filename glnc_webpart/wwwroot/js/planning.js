let calendar;
let currentView = 'month';
let currentDate = new Date();
let originalDeliveryData = null; // Store full original delivery data when editing

// Initialize the planning calendar
function initPlanningCalendar() {
    const calendarEl = document.getElementById('calendar');
    
    if (!calendarEl) {
        console.error('Calendar element not found!');
        return;
    }
    
    if (typeof FullCalendar === 'undefined') {
        console.error('FullCalendar library not loaded!');
        return;
    }
    
    calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'timeGridWeek',
        locale: 'fr',
        timeZone: 'Pacific/Noumea', // New Caledonia timezone (UTC+11)
        headerToolbar: false,
        height: 'auto',
        slotMinTime: '05:00:00',
        slotMaxTime: '19:00:00',
        slotDuration: '00:30:00',
        allDaySlot: false,
        weekends: true,
        firstDay: 1, // Monday
        editable: true,
        selectable: true,
        selectMirror: true,
        dayMaxEvents: true,
        events: function(fetchInfo, successCallback, failureCallback) {
            loadDeliveries(fetchInfo.start, fetchInfo.end, successCallback, failureCallback);
        },
        eventContent: function(arg) {
            // Custom event rendering to include status icon
            const event = arg.event;
            let statusIcon = null;
            
            // Try to get statusIcon from multiple places
            if (event.extendedProps) {
                statusIcon = event.extendedProps.statusIcon || event.extendedProps.statusicon;
            }
            if (!statusIcon && event.statusIcon) {
                statusIcon = event.statusIcon;
            }
            if (!statusIcon && event._def && event._def.extendedProps) {
                statusIcon = event._def.extendedProps.statusIcon;
            }
            
            const timeText = arg.timeText;
            let html = '';
            
            // Add status icon if available
            if (statusIcon && statusIcon !== '' && statusIcon.trim() !== '') {
                html += `<img src="${statusIcon}" class="delivery-status-icon" style="width: 16px; height: 16px; margin-right: 4px; vertical-align: middle; display: inline-block;" alt="status" />`;
            }
            
            // Add time if available
            if (timeText) {
                html += `<span class="fc-event-time">${timeText}</span> `;
            }
            
            // Add title
            html += `<span class="fc-event-title">${event.title}</span>`;
            
            return { html: html };
        },
        eventDidMount: function(info) {
            // Add status icon to delivery events
            // Try multiple ways to access statusIcon
            let statusIcon = null;
            
            // Check extendedProps first
            if (info.event.extendedProps) {
                statusIcon = info.event.extendedProps.statusIcon || 
                            info.event.extendedProps.statusicon;
            }
            
            // Also check if it's at the root level
            if (!statusIcon && info.event.statusIcon) {
                statusIcon = info.event.statusIcon;
            }
            
            // Check _def.extendedProps as fallback
            if (!statusIcon && info.event._def && info.event._def.extendedProps) {
                statusIcon = info.event._def.extendedProps.statusIcon;
            }
            
            if (statusIcon && statusIcon !== '' && statusIcon.trim() !== '') {
                // Check if image already exists to avoid duplicates
                if (info.el.querySelector('img.delivery-status-icon')) {
                    return;
                }
                
                const img = document.createElement('img');
                img.src = statusIcon;
                img.style.width = '16px';
                img.style.height = '16px';
                img.style.marginRight = '4px';
                img.style.verticalAlign = 'middle';
                img.style.display = 'inline-block';
                img.alt = 'status';
                img.className = 'delivery-status-icon';
                
                // Wait a bit for DOM to be ready, then insert
                setTimeout(function() {
                    // Find the time element - it's usually inside .fc-content > .fc-event-time
                    const timeElement = info.el.querySelector('.fc-event-time');
                    const contentElement = info.el.querySelector('.fc-content');
                    
                    // Insert image before the time element0
                    if (timeElement) {
                        // Insert before time element's parent (fc-content) or before time element itself
                        if (timeElement.parentElement && timeElement.parentElement === contentElement) {
                            contentElement.insertBefore(img, timeElement);
                        } else if (timeElement.parentElement) {
                            timeElement.parentElement.insertBefore(img, timeElement);
                        } else {
                            // Insert at the start of time element
                            timeElement.insertBefore(img, timeElement.firstChild);
                        }
                    } else if (contentElement) {
                        // No time element, insert at start of content
                        contentElement.insertBefore(img, contentElement.firstChild);
                    } else {
                        // Fallback: insert at the beginning of the event element
                        info.el.insertBefore(img, info.el.firstChild);
                    }
                }, 10);
            }
        },
        eventClick: function(info) {
            editDelivery(info.event);
        },
        select: function(info) {
            createDelivery(info.start, info.end, info);
        },
        eventDrop: function(info) {
            updateDeliveryTime(info.event);
        },
        eventResize: function(info) {
            updateDeliveryTime(info.event);
        }
    });

    calendar.render();
    updateDateRange();
    
    // Setup event listeners
    setupEventListeners();
}

// Setup event listeners for controls
function setupEventListeners() {
    // Driver filter
    document.getElementById('driverSelect').addEventListener('change', function() {
        refreshCalendar();
    });

    // Truck filter
    document.getElementById('truckSelect').addEventListener('change', function() {
        refreshCalendar();
    });

    // Navigation buttons
    document.getElementById('prevWeek').addEventListener('click', function() {
        calendar.prev();
        updateDateRange();
    });

    document.getElementById('nextWeek').addEventListener('click', function() {
        calendar.next();
        updateDateRange();
    });

    document.getElementById('todayBtn').addEventListener('click', function() {
        calendar.today();
        updateDateRange();
    });

    // View buttons
    document.querySelectorAll('.view-buttons button').forEach(btn => {
        btn.addEventListener('click', function() {
            const view = this.getAttribute('data-view');
            changeView(view);
            document.querySelectorAll('.view-buttons button').forEach(b => b.classList.remove('active'));
            this.classList.add('active');
        });
    });

    // Save button
    document.getElementById('saveBtn').addEventListener('click', function() {
        // This can be used for bulk operations if needed
        showToast('Calendar view saved', 'success');
    });

    // Modal save button
    document.getElementById('saveDeliveryBtn').addEventListener('click', function() {
        saveDelivery();
    });

    // Check for truck conflicts when truck or time changes
    document.getElementById('modalTruck').addEventListener('change', checkTruckConflict);
    document.getElementById('modalAppointment').addEventListener('change', checkTruckConflict);
    document.getElementById('modalLeave').addEventListener('change', checkTruckConflict);
    document.getElementById('modalDriver').addEventListener('change', checkTruckConflict);
    
    // Invoice image upload handler for OCR
    const invoiceImageInput = document.getElementById('invoiceImageInput');
    if (invoiceImageInput) {
        invoiceImageInput.addEventListener('change', function(e) {
            const file = e.target.files[0];
            if (file) {
                // Validate file type
                if (!file.type.startsWith('image/')) {
                    showToast('Veuillez sélectionner un fichier image.', 'error');
                    e.target.value = ''; // Clear the input
                    return;
                }
                
                // Validate file size (max 10MB)
                if (file.size > 10 * 1024 * 1024) {
                    showToast('L\'image est trop grande. Maximum 10MB.', 'error');
                    e.target.value = ''; // Clear the input
                    return;
                }
                
                handleInvoiceImageUpload(file);
            }
        });
    }
}

// Load deliveries from server
function loadDeliveries(start, end, successCallback, failureCallback) {
    const driverId = document.getElementById('driverSelect').value;
    const truckId = document.getElementById('truckSelect').value;

    $.ajax({
        url: '/Planning/GetDeliveries',
        type: 'GET',
        data: {
            start: start.toISOString(),
            end: end.toISOString(),
            driverId: driverId || null,
            truckId: truckId || null
        },
        success: function(events) {
            // Process events to ensure extendedProps are accessible
            if (events && events.length > 0) {
                events.forEach(function(event) {
                    // Ensure extendedProps exists and statusIcon is accessible
                    if (!event.extendedProps) {
                        event.extendedProps = {};
                    }
                    // Also check if statusIcon is at root level and move it to extendedProps
                    if (event.statusIcon && !event.extendedProps.statusIcon) {
                        event.extendedProps.statusIcon = event.statusIcon;
                    }
                });
            }
            successCallback(events);
        },
        error: function() {
            failureCallback();
            showToast('Error loading deliveries', 'error');
        }
    });
}

// Refresh calendar
function refreshCalendar() {
    calendar.refetchEvents();
}

// Update date range display
function updateDateRange() {
    const view = calendar.view;
    const start = view.activeStart;
    const end = view.activeEnd;
    
    const startStr = formatDate(start);
    const endStr = formatDate(end);
    
    document.getElementById('dateRange').textContent = `${startStr} - ${endStr}`;
}

// Format date for display
function formatDate(date) {
    const months = ['janv.', 'févr.', 'mars', 'avr.', 'mai', 'juin', 
                    'juil.', 'août', 'sept.', 'oct.', 'nov.', 'déc.'];
    const day = date.getDate();
    const month = months[date.getMonth()];
    const year = date.getFullYear();
    return `${day} ${month} ${year}`;
}

// Change calendar view
function changeView(view) {
    currentView = view;
    let fcView;
    
    switch(view) {
        case 'month':
            fcView = 'dayGridMonth';
            break;
        case 'week':
            fcView = 'timeGridWeek';
            break;
        case 'day':
            fcView = 'timeGridDay';
            break;
        case 'list':
            fcView = 'listWeek';
            break;
        default:
            fcView = 'timeGridWeek';
    }
    
    calendar.changeView(fcView);
    updateDateRange();
}

// Create new delivery
function createDelivery(start, end, selectInfo) {
    // Check if the selected date/time is in the past
    const now = new Date();
    const selectedStart = new Date(start);
    
    // Normalize times to seconds for accurate comparison
    const nowTime = now.getTime();
    const selectedTime = selectedStart.getTime();
    
    // Compare dates - if selected time is in the past, show warning and prevent modal
    if (selectedTime < nowTime) {
        // Unselect the calendar selection
        calendar.unselect();
        showToast('Cannot create delivery for a past date. Please select a current or future date.', 'warning');
        return; // Exit early - don't proceed with any further validation
    }

    // Get driver and truck from filter dropdowns
    const driverId = document.getElementById('driverSelect').value;
    const truckId = document.getElementById('truckSelect').value;
    
    // Validate that driver and truck are selected
    let validationError = false;
    let errorMessage = '';
    
    if (!driverId || driverId === '') {
        errorMessage = 'Please select a driver from the filter dropdown';
        validationError = true;
    }
    
    if (!truckId || truckId === '') {
        if (validationError) {
            errorMessage = 'Please select both a driver and a truck from the filter dropdowns';
        } else {
            errorMessage = 'Please select a truck from the filter dropdown';
        }
        validationError = true;
    }
    
    if (validationError) {
        // Unselect the calendar selection
        if (selectInfo && selectInfo.jsEvent) {
            calendar.unselect();
        }
        showToast(errorMessage, 'warning');
        return;
    }
    
    // Check for conflicts with in-transit deliveries BEFORE opening modal
    checkTruckConflictBeforeCreate(driverId, truckId, start, end, function(hasConflict) {
        if (hasConflict) {
            // Unselect the calendar selection
            if (selectInfo && selectInfo.jsEvent) {
                calendar.unselect();
            }
            showToast('This truck is already assigned to another driver during this time period. Please select a different truck or time.', 'error');
            return;
        }
        
        // No conflict, open the modal
        const modal = new bootstrap.Modal(document.getElementById('deliveryModal'));
        document.getElementById('modalTitle').textContent = 'Create Delivery';
        document.getElementById('deliveryForm').reset();
        document.getElementById('deliveryId').value = '0';
        
        // Set default times (disabled)
        const startStr = formatDateTimeLocal(start);
        const endStr = formatDateTimeLocal(end);
        const appointmentInput = document.getElementById('modalAppointment');
        const leaveInput = document.getElementById('modalLeave');
        appointmentInput.value = startStr;
        leaveInput.value = endStr;
        appointmentInput.disabled = true;
        leaveInput.disabled = true;
        
        // Set driver and truck (disabled)
        const driverSelect = document.getElementById('modalDriver');
        const truckSelect = document.getElementById('modalTruck');
        driverSelect.value = driverId;
        driverSelect.disabled = true;
        truckSelect.value = truckId;
        truckSelect.disabled = true;
        
        // Hide conflict warning
        document.getElementById('truckConflictWarning').style.display = 'none';
        
        // Hide delete button for new deliveries
        document.getElementById('deleteDeliveryBtn').style.display = 'none';
        
        // Reset original delivery data for new deliveries
        originalDeliveryData = null;
        
        // Reset invoice image input
        const invoiceImageInput = document.getElementById('invoiceImageInput');
        if (invoiceImageInput) {
            invoiceImageInput.value = '';
        }
        const ocrProgress = document.getElementById('ocrProgress');
        if (ocrProgress) {
            ocrProgress.style.display = 'none';
        }
        
        // Unselect the calendar selection before opening modal
        calendar.unselect();
        
        modal.show();
    });
}

// Check for truck conflicts before creating delivery (only checks in-transit deliveries)
function checkTruckConflictBeforeCreate(driverId, truckId, start, end, callback) {
    $.ajax({
        url: '/Planning/GetDeliveries',
        type: 'GET',
        data: {
            start: start.toISOString(),
            end: end.toISOString(),
            truckId: truckId,
            inTransitOnly: true // Only check in-transit deliveries
        },
        success: function(events) {
            // Check if truck is assigned to a different driver during this time
            const conflict = events.some(event => {
                // Check if assigned to different driver
                return event.driverId != driverId;
            });
            
            callback(conflict);
        },
        error: function() {
            // On error, allow creation (don't block)
            callback(false);
        }
    });
}

// Edit existing delivery
function editDelivery(event) {
    const modal = new bootstrap.Modal(document.getElementById('deliveryModal'));
    document.getElementById('modalTitle').textContent = 'Edit Delivery';
    
    // Load delivery details
    $.ajax({
        url: '/Delivery/Get',
        type: 'GET',
        data: { id: event.id },
        success: function(response) {
            const delivery = response.data || response;
            document.getElementById('deliveryId').value = delivery.id;
            
            // Store full original delivery data for preserving fields not in modal
            originalDeliveryData = delivery;
            
            // Set driver, truck, and dates (disabled)
            const driverSelect = document.getElementById('modalDriver');
            const truckSelect = document.getElementById('modalTruck');
            const appointmentInput = document.getElementById('modalAppointment');
            const leaveInput = document.getElementById('modalLeave');
            
            driverSelect.value = delivery.userId;
            driverSelect.disabled = true;
            truckSelect.value = delivery.truckId;
            truckSelect.disabled = true;
            appointmentInput.value = formatDateTimeLocal(parseNewCaledoniaDate(delivery.dateTimeAppointment));
            appointmentInput.disabled = true;
            leaveInput.value = formatDateTimeLocal(parseNewCaledoniaDate(delivery.dateTimeLeave));
            leaveInput.disabled = true;
            
            // Fill other fields (only fields that exist in the form)
            const form = document.getElementById('deliveryForm');
            const clientField = form.querySelector('[name="Client"]');
            const supplierField = form.querySelector('[name="SupplierId"]');
            const addressField = form.querySelector('[name="Address"]');
            const contactsField = form.querySelector('[name="Contacts"]');
            const invoiceField = form.querySelector('[name="Invoice"]');
            const weightField = form.querySelector('[name="Weight"]');
            
            if (clientField) clientField.value = delivery.client || '';
            if (supplierField) supplierField.value = delivery.supplierId || '';
            if (addressField) addressField.value = delivery.address || '';
            if (contactsField) contactsField.value = delivery.contacts || '';
            if (invoiceField) invoiceField.value = delivery.invoice || '';
            if (weightField) weightField.value = delivery.weight || '';
            
            // Hide conflict warning for editing (fields are disabled anyway)
            document.getElementById('truckConflictWarning').style.display = 'none';
            
            // Show delete button for existing deliveries
            document.getElementById('deleteDeliveryBtn').style.display = 'inline-block';
            
            // Reset invoice image input
            const invoiceImageInput = document.getElementById('invoiceImageInput');
            if (invoiceImageInput) {
                invoiceImageInput.value = '';
            }
            const ocrProgress = document.getElementById('ocrProgress');
            if (ocrProgress) {
                ocrProgress.style.display = 'none';
            }
            
            modal.show();
        },
        error: function() {
            showToast('Error loading delivery details', 'error');
        }
    });
}

// Save delivery
function saveDelivery() {
    const form = document.getElementById('deliveryForm');
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    // Build form data - only include fields from the modal
    const formData = $(form).serializeArray();
    
    // Convert to object
    const data = {};
    formData.forEach(function(field) {
        // Only include fields that are in the modal
        if (['Client', 'SupplierId', 'Address', 'Contacts', 'Invoice', 'Weight'].includes(field.name)) {
            data[field.name] = field.value;
        }
    });
    
    // Add disabled fields manually (disabled fields are not included in serialize)
    data['UserId'] = document.getElementById('modalDriver').value;
    data['TruckId'] = document.getElementById('modalTruck').value;
    data['DateTimeAppointment'] = document.getElementById('modalAppointment').value;
    data['DateTimeLeave'] = document.getElementById('modalLeave').value;
    
    // Set ReturnFlag to false by default (not needed for creating new delivery)
    data['ReturnFlag'] = 'false';

    // Ensure Id is set (0 for new deliveries)
    if (!data['Id'] || data['Id'] === '') {
        data['Id'] = '0';
    }
    
    // Validate required fields before sending
    if (!data['UserId'] || data['UserId'] === '') {
        showToast('Driver is required', 'error');
        return;
    }
    
    if (!data['TruckId'] || data['TruckId'] === '') {
        showToast('Truck is required', 'error');
        return;
    }
    
    if (!data['DateTimeAppointment']) {
        showToast('Appointment Date & Time is required', 'error');
        return;
    }
    
    if (!data['DateTimeLeave']) {
        showToast('Leave Date & Time is required', 'error');
        return;
    }
    
    if (!data['Client'] || data['Client'].trim() === '') {
        showToast('Client is required', 'error');
        return;
    }
    
    if (!data['SupplierId'] || data['SupplierId'] === '') {
        showToast('Supplier is required', 'error');
        return;
    }
    
    if (!data['Address'] || data['Address'].trim() === '') {
        showToast('Address is required', 'error');
        return;
    }
    
    if (!data['Contacts'] || data['Contacts'].trim() === '') {
        showToast('Contacts is required', 'error');
        return;
    }
    
    if (!data['Invoice'] || data['Invoice'].trim() === '') {
        showToast('Invoice is required', 'error');
        return;
    }
    
    if (!data['Weight'] || parseFloat(data['Weight']) <= 0) {
        showToast('Weight must be greater than 0', 'error');
        return;
    }

    // Get delivery ID from the form
    const deliveryId = document.getElementById('deliveryId').value;
    const currentClientName = data['Client'] ? data['Client'].trim() : '';
    
    // Determine if we're editing an existing delivery
    // If deliveryId exists and is not '0', we're editing - ALWAYS UPDATE
    // If deliveryId is '0' or missing, we're creating - CREATE NEW
    const isEditing = deliveryId && deliveryId !== '0' && deliveryId !== '';
    
    
    // Prepare the request
    let requestUrl;
    let requestData;
    let requestType;
    
    if (isEditing) {
        // ALWAYS UPDATE existing delivery when in edit mode
        requestUrl = '/Delivery/Edit';
        requestType = 'POST';
        
        // Build complete delivery object for update, preserving fields not in modal
        requestData = {
            Id: parseInt(deliveryId),
            UserId: parseInt(data['UserId']),
            TruckId: parseInt(data['TruckId']),
            DateTimeAppointment: toNewCaledoniaISOString(new Date(data['DateTimeAppointment'])),
            DateTimeLeave: toNewCaledoniaISOString(new Date(data['DateTimeLeave'])),
            SupplierId: parseInt(data['SupplierId']),
            Client: currentClientName,
            Address: data['Address'] ? data['Address'].trim() : '',
            Contacts: data['Contacts'] ? data['Contacts'].trim() : '',
            Invoice: data['Invoice'] ? data['Invoice'].trim() : '',
            Weight: parseFloat(data['Weight']),
            ReturnFlag: originalDeliveryData ? (originalDeliveryData.returnFlag || false) : (data['ReturnFlag'] === 'true' || data['ReturnFlag'] === true),
            // Preserve fields not in modal from original delivery (using camelCase from API response)
            DateTimeAccept: originalDeliveryData ? (originalDeliveryData.dateTimeAccept ? toNewCaledoniaISOString(parseNewCaledoniaDate(originalDeliveryData.dateTimeAccept)) : null) : null,
            DateTimeArrival: originalDeliveryData ? (originalDeliveryData.dateTimeArrival ? toNewCaledoniaISOString(parseNewCaledoniaDate(originalDeliveryData.dateTimeArrival)) : null) : null,
            Description: originalDeliveryData ? (originalDeliveryData.description || null) : null,
            SignClient: originalDeliveryData ? (originalDeliveryData.signClient || null) : null,
            SatisfactionClient: originalDeliveryData ? (originalDeliveryData.satisfactionClient || null) : null,
            Comment: originalDeliveryData ? (originalDeliveryData.comment || '') : '',
            InvoiceImage: originalDeliveryData ? (originalDeliveryData.invoiceImage || null) : null
        };
    } else {
        // CREATE new delivery via Planning/Create endpoint (only when creating from scratch)
        requestUrl = '/Planning/Create';
        requestType = 'POST';
        // Ensure Id is set to '0' for new deliveries
        requestData = Object.assign({}, data);
        requestData['Id'] = '0';
    }

    $.ajax({
        url: requestUrl,
        type: requestType,
        contentType: isEditing ? 'application/json' : 'application/x-www-form-urlencoded',
        data: isEditing ? JSON.stringify(requestData) : requestData,
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        success: function(response) {
            if (response.success) {
                const message = isEditing ? 'Livraison mise à jour avec succès' : 'Livraison réussie';
                showToast(message, 'success');
                bootstrap.Modal.getInstance(document.getElementById('deliveryModal')).hide();
                // Reset original delivery data
                originalDeliveryData = null;
                refreshCalendar();
            } else {
                let errorMsg = response.message || (isEditing ? 'Erreur lors de la mise à jour de la livraison' : 'Erreur lors de la création de la livraison');
                if (response.errors && Array.isArray(response.errors)) {
                    const errorList = response.errors.map(e => e.Message || e).join('; ');
                    errorMsg += ': ' + errorList;
                }
                showToast(errorMsg, 'error');
            }
        },
        error: function(xhr) {
            let errorMsg = 'An error occurred. Please try again.';
            if (xhr.responseJSON && xhr.responseJSON.message) {
                errorMsg = xhr.responseJSON.message;
            } else if (xhr.responseText) {
                try {
                    const error = JSON.parse(xhr.responseText);
                    errorMsg = error.message || errorMsg;
                } catch (e) {
                    // Ignore parse error
                }
            }
            showToast(errorMsg, 'error');
        }
    });
}

// Check for truck conflicts (for editing - returns true if no conflict, false if conflict)
function checkTruckConflict() {
    const truckId = document.getElementById('modalTruck').value;
    const driverId = document.getElementById('modalDriver').value;
    const appointment = document.getElementById('modalAppointment').value;
    const leave = document.getElementById('modalLeave').value;
    const deliveryId = document.getElementById('deliveryId').value;

    if (!truckId || !appointment || !leave) {
        document.getElementById('truckConflictWarning').style.display = 'none';
        return true; // No conflict if fields are empty
    }

    // For editing, we still check but don't block (just show warning)
    let hasConflict = false;
    
    // Check if truck is already assigned to another driver during this time (in-transit only)
    $.ajax({
        url: '/Planning/GetDeliveries',
        type: 'GET',
        async: false, // Make synchronous for immediate return
        data: {
            start: new Date(appointment).toISOString(),
            end: new Date(leave).toISOString(),
            truckId: truckId,
            inTransitOnly: true // Only check in-transit deliveries
        },
        success: function(events) {
            const conflict = events.some(event => {
                // Exclude current delivery if editing
                if (deliveryId && event.id == deliveryId) {
                    return false;
                }
                // Check if assigned to different driver
                return event.driverId != driverId;
            });

            hasConflict = conflict;
            if (conflict) {
                document.getElementById('truckConflictWarning').style.display = 'block';
            } else {
                document.getElementById('truckConflictWarning').style.display = 'none';
            }
        },
        error: function() {
            hasConflict = false; // Allow save if check fails
        }
    });

    return !hasConflict; // Return true if no conflict
}

// Update delivery time (when dragged or resized)
function updateDeliveryTime(event) {
    const deliveryId = event.id;
    const start = event.start;
    const end = event.end || start;

    // Get the full delivery data first
    $.ajax({
        url: '/Delivery/Get',
        type: 'GET',
        data: { id: deliveryId },
        success: function(response) {
            const delivery = response.data || response;
            
            // Update only the time fields (convert to UTC+11 format)
            delivery.dateTimeAppointment = toNewCaledoniaISOString(start);
            delivery.dateTimeLeave = toNewCaledoniaISOString(end);
            
            // Send update
            $.ajax({
                url: '/Delivery/Edit',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(delivery),
                headers: { 
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() 
                },
                success: function(editResponse) {
                    if (editResponse.success) {
                        showToast('Delivery updated successfully', 'success');
                    } else {
                        showToast(editResponse.message || 'Error updating delivery', 'error');
                        refreshCalendar(); // Revert changes
                    }
                },
                error: function() {
                    showToast('An error occurred. Please try again.', 'error');
                    refreshCalendar(); // Revert changes
                }
            });
        },
        error: function() {
            showToast('Error loading delivery', 'error');
            refreshCalendar(); // Revert changes
        }
    });
}

// Format datetime for input[type="datetime-local"]
// FullCalendar handles timezone conversion, so we just format the date
function formatDateTimeLocal(date) {
    if (!date) return '';
    
    // If date is a string, parse it first (assume it's in UTC+11 format from server)
    if (typeof date === 'string') {
        // Server sends dates as "yyyy-MM-ddTHH:mm:ss" - treat as UTC+11
        date = parseNewCaledoniaDate(date);
    }
    
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
}

// Parse date string from server (assumed to be in UTC+11) and convert to Date object
// Server sends dates as "yyyy-MM-ddTHH:mm:ss" without timezone info
function parseNewCaledoniaDate(dateString) {
    if (!dateString)    
    // Strategy 3: Look for lines that appear before address lines
    // Customer name often appears before the address
    for (let i = 0; i < lines.length; i++) {
        // Look for address indicators (numbers, street names, postal codes)
        if (/\d+.*(?:avenue|rue|street|bp|b\.p\.|postal|code)/i.test(lines[i])) {
            // Check previous lines for potential customer name
            for (let j = Math.max(0, i - 3); j < i; j++) {
                const candidate = lines[j];
                if (/^[A-ZÀÂÄÇÉÈÊËÏÎÔÖÙÛÜÆŒ\s'-]{5,50}$/.test(candidate) &&
                    !isExcluded(candidate) &&
                    !/^\d/.test(candidate)) { // Don't match lines starting with numbers
                    return candidate;
                }
            }
        }
    }
     return null;
    
    // Parse the date string (assumed to be in UTC+11)
    // Format: "yyyy-MM-ddTHH:mm:ss" or "yyyy-MM-dd HH:mm:ss"
    const normalized = dateString.replace(' ', 'T').split('.')[0]; // Remove milliseconds if present
    const parts = normalized.split('T');
    if (parts.length !== 2) return new Date(dateString);
    
    const datePart = parts[0].split('-');
    const timePart = parts[1].split(':');
    
    if (datePart.length !== 3 || timePart.length < 2) return new Date(dateString);
    
    const year = parseInt(datePart[0]);
    const month = parseInt(datePart[1]) - 1; // JavaScript months are 0-indexed
    const day = parseInt(datePart[2]);
    const hours = parseInt(timePart[0]);
    const minutes = parseInt(timePart[1] || 0);
    const seconds = parseInt(timePart[2] || 0);
    
    // Create date assuming it's in UTC+11, then convert to local Date object
    // We create it as if it's UTC, then adjust
    const utcDate = new Date(Date.UTC(year, month, day, hours, minutes, seconds));
    // The date string is in UTC+11, so we need to subtract 11 hours to get UTC
    const utcTime = utcDate.getTime() - (11 * 60 * 60 * 1000);
    return new Date(utcTime);
}

// Convert Date object to UTC+11 ISO string for server
// Takes a Date object and converts it to a string as if it's in UTC+11
function toNewCaledoniaISOString(date) {
    if (!date)    
    // Strategy 3: Look for lines that appear before address lines
    // Customer name often appears before the address
    for (let i = 0; i < lines.length; i++) {
        // Look for address indicators (numbers, street names, postal codes)
        if (/\d+.*(?:avenue|rue|street|bp|b\.p\.|postal|code)/i.test(lines[i])) {
            // Check previous lines for potential customer name
            for (let j = Math.max(0, i - 3); j < i; j++) {
                const candidate = lines[j];
                if (/^[A-ZÀÂÄÇÉÈÊËÏÎÔÖÙÛÜÆŒ\s'-]{5,50}$/.test(candidate) &&
                    !isExcluded(candidate) &&
                    !/^\d/.test(candidate)) { // Don't match lines starting with numbers
                    return candidate;
                }
            }
        }
    }
     return null;
    
    // Get the date components in UTC
    const utcTime = date.getTime();
    // Add 11 hours to convert to UTC+11 representation
    const ncTime = utcTime + (11 * 60 * 60 * 1000);
    const ncDate = new Date(ncTime);
    
    // Format as ISO string (UTC+11)
    const year = ncDate.getUTCFullYear();
    const month = String(ncDate.getUTCMonth() + 1).padStart(2, '0');
    const day = String(ncDate.getUTCDate()).padStart(2, '0');
    const hours = String(ncDate.getUTCHours()).padStart(2, '0');
    const minutes = String(ncDate.getUTCMinutes()).padStart(2, '0');
    const seconds = String(ncDate.getUTCSeconds()).padStart(2, '0');
    
    return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
}

// ============================================
// OCR Invoice Extraction Functions
// ============================================

// Extract customer name from OCR text
function extractCustomerName(text) {
    if (!text) {
        console.log('extractCustomerName: No text provided');
        return null;
    }
    
    console.log('extractCustomerName: Starting extraction, text length:', text.length);
    
    // Split into lines and clean them
    const lines = text.split(/\r?\n/)
        .map(line => line.trim())
        .filter(line => line.length > 0);
    
    console.log('extractCustomerName: Total lines after cleaning:', lines.length);
    console.log('extractCustomerName: First 10 lines:', lines.slice(0, 10));
    
    // Excluded words/phrases that should NOT be considered as customer names
    const excluded = [
        'SODEVIA', 'FACTURE', 'LIVRAISON', 'COMMANDE', 'COMPTABILITE', 'DIRECTION',
        'REPRESENTANT', 'TRANSPORTEUR', 'CODE CLIENT', 'REFERENCE', 'CLIENT',
        'NOUMEA', 'NOUVELLE', 'CALEDONIE', 'TOTAL', 'PRIX', 'MONTANT', 'SOMME',
        'FRANCS', 'XPF', 'ARRETE', 'PRESENTE', 'REGLEMENT', 'ESPECE', 'PAGE'
    ];
    
    // Helper function to check if a line should be excluded
    function isExcluded(line) {
        const upperLine = line.toUpperCase();
        return excluded.some(ex => {
            if (upperLine === ex) return true;
            // Check if excluded word appears as standalone word
            const regex = new RegExp('\\b' + ex.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '\\b', 'i');
            return regex.test(upperLine);
        });
    }
    
    // Strategy 0: Extract from "Facture" line (e.g., "Facture FA0540506 KORAIL NORMANDIE SUPERMARKET BARQUET")
    // This is a common pattern where customer name appears after invoice number
    console.log('extractCustomerName: Strategy 0 - Looking for "Facture" line pattern...');
    for (let i = 0; i < lines.length; i++) {
        const line = lines[i];
        // Pattern: "Facture" followed by invoice code/number, then customer name
        const factureMatch = line.match(/facture\s+[A-Z0-9]+\s+([A-ZÀÂÄÇÉÈÊËÏÎÔÖÙÛÜÆŒ\s'-]{8,50})/i);
        if (factureMatch) {
            const name = factureMatch[1].trim();
            console.log('extractCustomerName: Found "Facture" line at index', i, ':', line);
            console.log('extractCustomerName: Extracted name:', name);
            // Validate it's not excluded and looks like a business name
            if (!isExcluded(name) && 
                name.length >= 8 && 
                name.length <= 50 &&
                (/(?:SUPERMARKET|SARL|SAS|SOCIETE|MARCHE|GROS|BOULARI|KORAIL|THIRIET|ALIMENTAIRE|OCEANIENNE|NORMANDIE|APOGOTI|BALLANDE)/i.test(name) ||
                 name.length >= 12)) {
                console.log('extractCustomerName: Strategy 0 SUCCESS - Returning:', name);
                return name;
            }
        }
    }
    
    // Strategy 1: Find lines that appear BEFORE address lines
    // Customer name is ALWAYS before the address on these invoices
    console.log('extractCustomerName: Strategy 1 - Looking for address lines...');
    for (let i = 0; i < lines.length; i++) {
        // Look for address indicators (postal codes, street names, BP, etc.)
        const isAddressLine = /\d{5}.*(?:NOUMEA|MONT|DUMBEA|NORMANDIE|BP|B\.P\.|AVENUE|RUE|LOT|SECTION|VILLA|RESIDENCE)/i.test(lines[i]) ||
            /(?:BP|B\.P\.)\s*\d+/i.test(lines[i]) ||
            /(?:AVENUE|RUE|LOT|SECTION|VILLA|RESIDENCE).*[A-Z]/i.test(lines[i]);
        
        if (isAddressLine) {
            console.log('extractCustomerName: Found address line at index', i, ':', lines[i]);
            // Check previous 1-5 lines for customer name
            for (let j = Math.max(0, i - 5); j < i; j++) {
                const candidate = lines[j];
                console.log('extractCustomerName: Checking candidate:', candidate);
                
                // Must be all uppercase, 8-50 chars, no numbers at start
                if (/^[A-ZÀÂÄÇÉÈÊËÏÎÔÖÙÛÜÆŒ\s'-]{8,50}$/.test(candidate) &&
                    !isExcluded(candidate) &&
                    !/^\d/.test(candidate)) {
                    console.log('extractCustomerName: Candidate matches pattern:', candidate);
                    // Additional validation: should look like a business name
                    // Contains business terms OR is reasonably long
                    if (/(?:SUPERMARKET|SARL|SAS|SOCIETE|MARCHE|GROS|BOULARI|KORAIL|THIRIET|ALIMENTAIRE|OCEANIENNE|NORMANDIE|APOGOTI|BALLANDE)/i.test(candidate) ||
                        candidate.length >= 12) {
                        console.log('extractCustomerName: Strategy 1 SUCCESS - Returning:', candidate);
                        return candidate;
                    }
                }
            }
        }
    }
    
    // Strategy 2: Look for CODE + NAME pattern (e.g., "39801 MMR BOITEUX J")
    console.log('extractCustomerName: Strategy 2 - Looking for CODE + NAME pattern...');
    for (const line of lines) {
        const match = line.match(/^\d{3,6}\s+([A-ZÀÂÄÇÉÈÊËÏÎÔÖÙÛÜÆŒ\s'-]{8,50})$/);
        if (match) {
            const name = match[1].trim();
            if (!/^\d+$/.test(name) && name.length >= 8 && !isExcluded(name)) {
                console.log('extractCustomerName: Strategy 2 SUCCESS - Returning:', name);
                return name;
            }
        }
    }
    
    // Strategy 3: Look after invoice metadata (Code Client, Représentant, etc.)
    console.log('extractCustomerName: Strategy 3 - Looking after invoice metadata...');
    for (let i = 0; i < lines.length; i++) {
        if (/(?:code\s+client|repr[ée]sentant|r[ée]f[ée]rence\s+commande)/i.test(lines[i])) {
            console.log('extractCustomerName: Found metadata line at index', i, ':', lines[i]);
            // Check next 3-8 lines for customer name
            for (let j = i + 1; j < Math.min(i + 8, lines.length); j++) {
                const candidate = lines[j];
                
                // Try CODE + NAME pattern first
                const codeNameMatch = candidate.match(/^\d{3,6}\s+([A-ZÀÂÄÇÉÈÊËÏÎÔÖÙÛÜÆŒ\s'-]{8,50})$/);
                if (codeNameMatch && !isExcluded(codeNameMatch[1].trim())) {
                    console.log('extractCustomerName: Strategy 3 SUCCESS (CODE+NAME) - Returning:', codeNameMatch[1].trim());
                    return codeNameMatch[1].trim();
                }
                
                // Then try standalone all-caps line
                if (/^[A-ZÀÂÄÇÉÈÊËÏÎÔÖÙÛÜÆŒ\s'-]{8,50}$/.test(candidate) &&
                    !isExcluded(candidate) &&
                    !/^\d/.test(candidate)) {
                    // Check if next line looks like an address (validates it's customer name)
                    if (j + 1 < lines.length) {
                        const nextLine = lines[j + 1];
                        if (/\d{5}|(?:BP|B\.P\.|AVENUE|RUE|LOT|SECTION|VILLA|RESIDENCE)/i.test(nextLine)) {
                            console.log('extractCustomerName: Strategy 3 SUCCESS - Returning:', candidate);
                            return candidate;
                        }
                    }
                }
            }
        }
    }
    
    // Strategy 4: Simple pattern - all uppercase line 8-50 chars, not excluded
    // This is a fallback for cases where context is unclear
    console.log('extractCustomerName: Strategy 4 - Fallback pattern matching...');
    const candidates = [];
    for (let i = 0; i < lines.length; i++) {
        const line = lines[i];
        if (/^[A-ZÀÂÄÇÉÈÊËÏÎÔÖÙÛÜÆŒ\s'-]{8,50}$/.test(line) &&
            !isExcluded(line) &&
            !/^\d/.test(line) &&
            !/^(TOTAL|PRIX|MONTANT|SOMME|FRANCS|XPF|ARRETE|PRESENTE)/i.test(line)) {
            // Prefer lines that contain business-related terms
            if (/(?:SUPERMARKET|SARL|SAS|SOCIETE|MARCHE|GROS|BOULARI|KORAIL|THIRIET|ALIMENTAIRE|OCEANIENNE|NORMANDIE|APOGOTI|BALLANDE)/i.test(line)) {
                console.log('extractCustomerName: Strategy 4 SUCCESS (business term) - Returning:', line);
                return line; // Return immediately if it has business terms
            }
            candidates.push({ line: line, index: i });
        }
    }
    
    // If we found candidates, return the longest one (likely the full business name)
    if (candidates.length > 0) {
        candidates.sort((a, b) => b.line.length - a.line.length);
        console.log('extractCustomerName: Strategy 4 SUCCESS (longest candidate) - Returning:', candidates[0].line);
        return candidates[0].line;
    }
    
    console.log('extractCustomerName: FAILED - No customer name found');
    return null;
}

// Extract weight from OCR text
function extractWeight(text) {
    if (!text) {
        console.log('extractWeight: No text provided');
        return null;
    }
    
    console.log('extractWeight: Starting extraction, text length:', text.length);
    
    // Split into lines
    const lines = text.split(/\r?\n/)
        .map(line => line.trim())
        .filter(line => line.length > 0);
    
    console.log('extractWeight: Total lines:', lines.length);
    
    // Strategy 1: Look for "Poids Livré" - HIGHEST PRIORITY (red-marked section)
    // Extract from the line DIRECTLY BELOW "Poids Livré"
    console.log('extractWeight: Strategy 1 - Looking for "Poids Livré" (HIGHEST PRIORITY - red-marked section)...');
    for (let i = 0; i < lines.length; i++) {
        if (/poids\s+livr[ée]/i.test(lines[i])) {
            const poidsLivréMatch = /poids\s+livr[ée]/i.exec(lines[i]);
            const poidsLivréLineIndex = i;
            const poidsLivréCharIndex = poidsLivréMatch ? lines[i].indexOf(poidsLivréMatch[0]) : -1;
            
            console.log('extractWeight: Found "Poids Livré" at line', i, ':', lines[i]);
            console.log('extractWeight: [COORDINATES] "Poids Livré" location:');
            console.log('extractWeight:   - Line index:', poidsLivréLineIndex);
            console.log('extractWeight:   - Character position in line:', poidsLivréCharIndex);
            console.log('extractWeight:   - Full line content:', lines[i]);
            
            // PRIORITY 1: Check the line DIRECTLY BELOW "Poids Livré" (red-marked section)
            if (i + 1 < lines.length) {
                const lineBelow = lines[i + 1];
                const weightLineIndex = i + 1;
                
                console.log('extractWeight: ===== EXTRACTING FROM RED-MARKED SECTION (line below "Poids Livré") =====');
                console.log('extractWeight: Line content:', lineBelow);
                console.log('extractWeight: Line index:', weightLineIndex);
                
                // Extract ALL decimal numbers from this line
                // Pattern: XX,XXX or XX.XXX (2-5 digits, comma/dot, 1-3 digits)
                // Handle formats like: 103,504, 37,63, 19,584, 19, 584 (with spaces), etc.
                // First try: decimals with possible spaces (e.g., "19, 584", "51, 958")
                let allDecimals = lineBelow.match(/\b(\d{2,5}\s*[,\.]\s*\d{1,3})\b/g);
                // If no match, try without spaces
                if (!allDecimals || allDecimals.length === 0) {
                    allDecimals = lineBelow.match(/\b(\d{2,5}[,\.]\d{1,3})\b/g);
                }
                
                if (allDecimals && allDecimals.length > 0) {
                    console.log('extractWeight: [Strategy 1] Found decimal(s) on line below "Poids Livré":', allDecimals);
                    
                    // Find the leftmost decimal (first one in the line)
                    let leftmostDecimal = null;
                    let leftmostIndex = Infinity;
                    let leftmostEndIndex = -1;
                    
                    for (const decimalStr of allDecimals) {
                        const indexInLine = lineBelow.indexOf(decimalStr);
                        if (indexInLine < leftmostIndex) {
                            leftmostIndex = indexInLine;
                            leftmostEndIndex = indexInLine + decimalStr.length;
                            leftmostDecimal = decimalStr;
                        }
                    }
                    
                    if (leftmostDecimal) {
                        const cleanNum = leftmostDecimal.replace(/\s/g, '').replace(',', '.');
                        const numValue = parseFloat(cleanNum);
                        
                        // Log complete coordinate information
                        console.log('extractWeight: [COORDINATES] Weight value location:');
                        console.log('extractWeight:   - "Poids Livré" line index:', poidsLivréLineIndex);
                        console.log('extractWeight:   - Weight value line index:', weightLineIndex);
                        console.log('extractWeight:   - Character start position:', leftmostIndex);
                        console.log('extractWeight:   - Character end position:', leftmostEndIndex);
                        console.log('extractWeight:   - Value length:', leftmostDecimal.length);
                        console.log('extractWeight:   - Extracted value:', leftmostDecimal);
                        console.log('extractWeight:   - Parsed value:', numValue);
                        console.log('extractWeight:   - Full line content:', lineBelow);
                        console.log('extractWeight:   - Context (50 chars before/after):', 
                            lineBelow.substring(Math.max(0, leftmostIndex - 50), Math.min(lineBelow.length, leftmostEndIndex + 50)));
                        
                        console.log('extractWeight: [Strategy 1] Leftmost decimal found:', leftmostDecimal, '->', numValue, 
                            `(at position ${leftmostIndex} in line)`);
                        
                        // Validate: reasonable weight range (0.1 to 10000 kg)
                        if (numValue >= 0.1 && numValue <= 10000) {
                            console.log('extractWeight: Strategy 1 SUCCESS (red-marked section) - Returning:', numValue.toFixed(2));
                            console.log('extractWeight: [FINAL COORDINATES]');
                            console.log('extractWeight:   Line[' + poidsLivréLineIndex + ']: "Poids Livré"');
                            console.log('extractWeight:   Line[' + weightLineIndex + '][' + leftmostIndex + '-' + leftmostEndIndex + ']: "' + leftmostDecimal + '"');
                            return numValue.toFixed(2);
                        } else {
                            console.log('extractWeight: [Strategy 1] Value out of range:', numValue);
                        }
                    }
                } else {
                    console.log('extractWeight: [Strategy 1] No decimals found on line below "Poids Livré"');
                }
                
                // Fallback: Check for integers if no decimals found
                // Handle cases where comma is missed by OCR (e.g., "19584" instead of "19,584")
                const allIntegers = lineBelow.match(/\b(\d{1,6})\b/g);
                if (allIntegers && allIntegers.length > 0) {
                    console.log('extractWeight: [Strategy 1] No decimals found, checking integers:', allIntegers);
                    
                    // Collect all valid integers with their positions
                    const validIntegers = [];
                    for (const intStr of allIntegers) {
                        const numValue = parseInt(intStr, 10);
                        // Validate: reasonable weight range, exclude years
                        if (numValue >= 1 && numValue <= 10000 && !/^(19|20)\d{2}$/.test(intStr)) {
                            const indexInLine = lineBelow.indexOf(intStr);
                            validIntegers.push({
                                value: numValue,
                                raw: intStr,
                                startIndex: indexInLine,
                                endIndex: indexInLine + intStr.length
                            });
                        }
                    }
                    
                    if (validIntegers.length > 0) {
                        // If multiple valid integers found, select the LARGEST one (most likely the weight)
                        // This handles cases like "19584" where comma might be missed
                        validIntegers.sort((a, b) => b.value - a.value);
                        const selected = validIntegers[0];
                        
                        // Convert to decimal format only if the integer is very large (likely missing comma)
                        // This handles cases where comma is missed: "19584" -> "19.584"
                        // But preserves valid weights like "1474" -> "1474.00"
                        let finalValue = selected.value;
                        if (selected.value >= 10000) {
                            // Very large integer: likely missing comma: "19584" should be "19,584"
                            finalValue = selected.value / 1000;
                            console.log('extractWeight: [Strategy 1] Very large integer detected, converting:', selected.value, '->', finalValue);
                        } else if (selected.value >= 1000 && selected.value < 10000) {
                            // Large integer: might be missing comma, but could also be valid weight
                            // Check if there are other smaller integers - if so, this large one is likely the weight
                            // Otherwise, it's probably a valid weight as-is
                            if (validIntegers.length > 1) {
                                // Multiple integers found - the largest is likely the weight (missing comma)
                                finalValue = selected.value / 1000;
                                console.log('extractWeight: [Strategy 1] Large integer with multiple candidates, converting:', selected.value, '->', finalValue);
                            } else {
                                // Single integer - likely a valid weight as-is
                                console.log('extractWeight: [Strategy 1] Single large integer, using as-is:', selected.value);
                            }
                        }
                        
                        // Log coordinates for integer
                        console.log('extractWeight: [COORDINATES] Weight value location (integer):');
                        console.log('extractWeight:   - "Poids Livré" line index:', poidsLivréLineIndex);
                        console.log('extractWeight:   - Weight value line index:', weightLineIndex);
                        console.log('extractWeight:   - Character start position:', selected.startIndex);
                        console.log('extractWeight:   - Character end position:', selected.endIndex);
                        console.log('extractWeight:   - Extracted value:', selected.raw);
                        console.log('extractWeight:   - Parsed value:', selected.value);
                        console.log('extractWeight:   - Final converted value:', finalValue);
                        console.log('extractWeight: Strategy 1 SUCCESS (red-marked section, integer) - Returning:', finalValue.toFixed(2));
                        console.log('extractWeight: [FINAL COORDINATES]');
                        console.log('extractWeight:   Line[' + poidsLivréLineIndex + ']: "Poids Livré"');
                        console.log('extractWeight:   Line[' + weightLineIndex + '][' + selected.startIndex + '-' + selected.endIndex + ']: "' + selected.raw + '"');
                        return finalValue.toFixed(2);
                    }
                }
            }
            
            // PRIORITY 2: Check current line (if weight is on same line as "Poids Livré")
            const sameLineMatch = lines[i].match(/poids\s+livr[ée][\s|:]*(\d{2,5}[,\.]\d{1,3})/i);
            if (sameLineMatch) {
                const weight = sameLineMatch[1].replace(/\s/g, '').replace(',', '.');
                const numWeight = parseFloat(weight);
                const weightStartIndex = lines[i].indexOf(sameLineMatch[1]);
                const weightEndIndex = weightStartIndex + sameLineMatch[1].length;
                
                console.log('extractWeight: [COORDINATES] Weight on same line as "Poids Livré":');
                console.log('extractWeight:   - Line index:', i);
                console.log('extractWeight:   - Character start position:', weightStartIndex);
                console.log('extractWeight:   - Character end position:', weightEndIndex);
                console.log('extractWeight:   - Extracted value:', sameLineMatch[1]);
                
                if (numWeight >= 0.1 && numWeight <= 10000) {
                    console.log('extractWeight: Strategy 1 SUCCESS (same line) - Returning:', numWeight.toFixed(2));
                    return numWeight.toFixed(2);
                }
            }
        }
    }
    
    // Strategy 0: Look for weight in bottom-left section (near totals) - FALLBACK
    // Weight appears as decimal XX,XX or XX.XX in the left column near summary section
    // This is separate from the "Poids Livré" label
    console.log('extractWeight: Strategy 0 - Looking for weight in bottom-left section (near totals)...');
    
    // Find the summary section (TOTAL, ARRETE, etc.)
    let strategy0TotalIndex = -1;
    let strategy0ArreteIndex = -1;
    for (let i = 0; i < lines.length; i++) {
        if (/^TOTAL\s*$/i.test(lines[i])) {
            strategy0TotalIndex = i;
            console.log('extractWeight: [Strategy 0] Found TOTAL at line', i);
        }
        if (/arrete\s+la\s+presente\s+facture/i.test(lines[i])) {
            strategy0ArreteIndex = i;
            console.log('extractWeight: [Strategy 0] Found ARRETE at line', i);
            break;
        }
    }
    
    // Search in the bottom section (from TOTAL to ARRETE or end of file)
    if (strategy0TotalIndex !== -1 || strategy0ArreteIndex !== -1) {
        const searchStart = strategy0TotalIndex !== -1 ? strategy0TotalIndex : Math.max(0, strategy0ArreteIndex - 10);
        const searchEnd = strategy0ArreteIndex !== -1 ? strategy0ArreteIndex : lines.length;
        
        console.log('extractWeight: [Strategy 0] Searching bottom section from line', searchStart, 'to', searchEnd);
        
        // Collect all decimal numbers found in BOTTOM-LEFT column
        // Priority: numbers at the very start of lines (leftmost position)
        const leftColumnDecimals = [];
        
        // Search from bottom to top (prioritize most bottom-left)
        for (let i = searchEnd - 1; i >= searchStart; i--) {
            const line = lines[i];
            const trimmedLine = line.trim();
            
            // PRIORITY 1: Look for decimal numbers at the VERY START of the line (leftmost)
            // Pattern: XX,XX or XX.XX (2-5 digits, comma/dot, 1-3 digits)
            // Allow minimal leading whitespace (0-3 spaces) - true left column
            const strictLeftMatch = line.match(/^\s{0,3}(\d{2,5}[,\.]\d{1,3})\b/);
            if (strictLeftMatch) {
                const numStr = strictLeftMatch[1];
                const cleanNum = numStr.replace(/\s/g, '').replace(',', '.');
                const numValue = parseFloat(cleanNum);
                const leadingSpaces = strictLeftMatch[0].length - numStr.length;
                
                console.log('extractWeight: [Strategy 0] Found BOTTOM-LEFT decimal at line', i, ':', numStr, '->', numValue, 
                    `(leading spaces: ${leadingSpaces}, position: leftmost)`);
                
                // Validate: reasonable weight range (0.1 to 10000 kg)
                if (numValue >= 0.1 && numValue <= 10000) {
                    leftColumnDecimals.push({
                        lineIndex: i,
                        raw: numStr,
                        value: numValue,
                        lineContent: line,
                        leadingSpaces: leadingSpaces,
                        position: 'leftmost',
                        distanceFromBottom: searchEnd - i - 1
                    });
                    console.log('extractWeight: [Strategy 0] ✓ Valid BOTTOM-LEFT candidate:', numStr, '=', numValue, 
                        `at line ${i} (${searchEnd - i - 1} lines from bottom)`);
                }
            }
            
            // PRIORITY 2: Check for numbers in first 5 characters (still left column)
            // This handles cases with minimal formatting/whitespace
            if (line.length > 0) {
                const earlyMatch = line.substring(0, 8).match(/(\d{2,5}[,\.]\d{1,3})\b/);
                if (earlyMatch && earlyMatch.index !== undefined && earlyMatch.index <= 3) {
                    const numStr = earlyMatch[1];
                    const cleanNum = numStr.replace(/\s/g, '').replace(',', '.');
                    const numValue = parseFloat(cleanNum);
                    
                    // Avoid duplicates
                    if (!leftColumnDecimals.some(c => c.raw === numStr && c.lineIndex === i)) {
                        if (numValue >= 0.1 && numValue <= 10000) {
                            leftColumnDecimals.push({
                                lineIndex: i,
                                raw: numStr,
                                value: numValue,
                                lineContent: line,
                                leadingSpaces: earlyMatch.index,
                                position: 'early-left',
                                distanceFromBottom: searchEnd - i - 1
                            });
                            console.log('extractWeight: [Strategy 0] ✓ Valid candidate (early-left):', numStr, '=', numValue, 
                                `at line ${i} (${searchEnd - i - 1} lines from bottom)`);
                        }
                    }
                }
            }
        }
        
        // Display all found candidates
        console.log('extractWeight: [Strategy 0] All left-column decimals found:', leftColumnDecimals.length);
        leftColumnDecimals.forEach((candidate, idx) => {
            console.log(`extractWeight: [Strategy 0] Candidate ${idx + 1}:`, candidate.raw, '=', candidate.value, 
                `(line ${candidate.lineIndex}: "${candidate.lineContent}")`);
        });
        
        // Select the best candidate from bottom-left section
        if (leftColumnDecimals.length > 0) {
            // Sort by priority:
            // 1. Most bottom (lowest line index = closest to bottom)
            // 2. Most left (fewest leading spaces)
            // 3. Leftmost position (leftmost > early-left)
            leftColumnDecimals.sort((a, b) => {
                // First priority: distance from bottom (closer to bottom = higher priority)
                if (a.distanceFromBottom !== b.distanceFromBottom) {
                    return a.distanceFromBottom - b.distanceFromBottom; // Lower distance = higher priority
                }
                // Second priority: leading spaces (fewer = more left)
                if (a.leadingSpaces !== b.leadingSpaces) {
                    return a.leadingSpaces - b.leadingSpaces;
                }
                // Third priority: position type (leftmost > early-left)
                if (a.position === 'leftmost' && b.position !== 'leftmost') return -1;
                if (b.position === 'leftmost' && a.position !== 'leftmost') return 1;
                return 0;
            });
            
            const bestCandidate = leftColumnDecimals[0];
            console.log('extractWeight: [Strategy 0] Selected BOTTOM-LEFT candidate:', 
                bestCandidate.raw, '=', bestCandidate.value, 
                `at line ${bestCandidate.lineIndex} (${bestCandidate.distanceFromBottom} lines from bottom, ` +
                `${bestCandidate.leadingSpaces} leading spaces, position: ${bestCandidate.position})`);
            console.log('extractWeight: Strategy 0 SUCCESS (bottom-left section) - Returning:', bestCandidate.value.toFixed(2));
            return bestCandidate.value.toFixed(2);
        } else {
            console.log('extractWeight: [Strategy 0] No valid bottom-left decimals found in bottom section');
        }
    } else {
        console.log('extractWeight: [Strategy 0] No summary section (TOTAL/ARRETE) found, skipping bottom-left search');
    }
    
    // Strategy 2: Look for "Poids" (without "Livré") - sometimes OCR misses "Livré"
    console.log('extractWeight: Strategy 2 - Looking for "Poids" (without Livré)...');
    for (let i = 0; i < lines.length; i++) {
        if (/^poids\s*:?/i.test(lines[i]) && !/livr/i.test(lines[i])) {
            console.log('extractWeight: Found "Poids" at line', i, ':', lines[i]);
            const numberMatch = lines[i].match(/poids\s*:?\s*(\d+[,\.]\d{1,3})/i);
            if (numberMatch) {
                const weight = numberMatch[1].replace(/\s/g, '').replace(',', '.');
                const numWeight = parseFloat(weight);
                if (numWeight >= 0.1 && numWeight <= 10000) {
                    console.log('extractWeight: Strategy 2 SUCCESS - Returning:', numWeight.toFixed(2));
                    return numWeight.toFixed(2);
                }
            }
        }
    }
    
    // Strategy 3: Look for "Quantité" in product table (often equals weight for single-item invoices)
    console.log('extractWeight: Strategy 3 - Looking for "Quantité"...');
    const quantiteMatches = [...text.matchAll(/quantit[ée]\s*:?\s*(\d+[,\.]\d{1,3})/gi)];
    if (quantiteMatches.length > 0) {
        console.log('extractWeight: Found', quantiteMatches.length, 'quantité matches');
        // Take the last "Quantité" value (often the total)
        const lastMatch = quantiteMatches[quantiteMatches.length - 1];
        const weight = lastMatch[1].replace(/\s/g, '').replace(',', '.');
        const numWeight = parseFloat(weight);
        console.log('extractWeight: Last quantité value:', weight, '->', numWeight);
        if (numWeight >= 0.1 && numWeight <= 10000) {
            console.log('extractWeight: Strategy 3 SUCCESS - Returning:', numWeight.toFixed(2));
            return numWeight.toFixed(2);
        }
    }
    
    // Strategy 4: Last resort - find decimal numbers in summary section
    // Look for numbers between "TOTAL" and "ARRETE LA PRESENTE FACTURE"
    console.log('extractWeight: Strategy 4 - Looking in summary section...');
    let totalIndex = -1;
    let arreteIndex = -1;
    for (let i = 0; i < lines.length; i++) {
        if (/^TOTAL\s*$/i.test(lines[i])) {
            totalIndex = i;
            console.log('extractWeight: Found TOTAL at line', i);
        }
        if (/arrete\s+la\s+presente\s+facture/i.test(lines[i])) {
            arreteIndex = i;
            console.log('extractWeight: Found ARRETE at line', i);
            break;
        }
    }
    
    if (totalIndex !== -1) {
        const searchEnd = arreteIndex !== -1 ? arreteIndex : lines.length;
        // Search backwards from "ARRETE" or end of file for decimal numbers
        for (let i = searchEnd - 1; i >= totalIndex; i--) {
            const match = lines[i].match(/(\d{1,3}[,\.]\d{1,3})/);
            if (match) {
                const weight = match[1].replace(/\s/g, '').replace(',', '.');
                const numWeight = parseFloat(weight);
                console.log('extractWeight: Found number in summary:', weight, '->', numWeight);
                // Prefer smaller numbers (weights are usually < 1000 kg)
                if (numWeight >= 0.1 && numWeight <= 1000) {
                    console.log('extractWeight: Strategy 4 SUCCESS - Returning:', numWeight.toFixed(2));
                    return numWeight.toFixed(2);
                }
            }
        }
    }
    
    console.log('extractWeight: FAILED - No weight found');
    return null;
}

// Handle invoice image upload and OCR processing
function handleInvoiceImageUpload(file) {
    if (!file) {
        console.error('handleInvoiceImageUpload: No file provided');
        return;
    }
    
    console.log('handleInvoiceImageUpload: File selected:', file.name, 'Size:', file.size, 'Type:', file.type);
    
    // Check if Tesseract is available
    if (typeof Tesseract === 'undefined') {
        console.error('handleInvoiceImageUpload: Tesseract.js is not loaded!');
        alert('Erreur: Tesseract.js n\'est pas chargé. Veuillez recharger la page.');
        return;
    }
    
    console.log('handleInvoiceImageUpload: Tesseract.js is available');
    
    // Show progress indicator
    const progressDiv = document.getElementById('ocrProgress');
    if (progressDiv) {
        progressDiv.style.display = 'block';
    }
    
    // Read file as data URL
    const reader = new FileReader();
    reader.onerror = function(error) {
        console.error('handleInvoiceImageUpload: FileReader error:', error);
        if (progressDiv) progressDiv.style.display = 'none';
        alert('Erreur lors de la lecture du fichier.');
    };
    
    reader.onload = async function() {
        try {
            console.log('handleInvoiceImageUpload: File loaded, starting OCR...');
            console.log('handleInvoiceImageUpload: File data URL length:', reader.result ? reader.result.length : 0);
            
            // Run OCR with French language
            const result = await Tesseract.recognize(
                reader.result, 
                'fra', // French language
                {
                    logger: m => {
                        if (m.status === 'recognizing text') {
                            console.log(`OCR Progress: ${Math.round(m.progress * 100)}%`);
                        } else {
                            console.log('OCR Status:', m.status, m);
                        }
                    }
                }
            );
            
            const text = result.data.text;
            console.log('handleInvoiceImageUpload: OCR completed');
            console.log('handleInvoiceImageUpload: Extracted text length:', text ? text.length : 0);
            console.log('handleInvoiceImageUpload: Full OCR text:', text);
            console.log('handleInvoiceImageUpload: First 500 chars:', text ? text.substring(0, 500) : 'null');
            
            // Extract customer name and weight
            console.log('handleInvoiceImageUpload: Starting extraction...');
            const customerName = extractCustomerName(text);
            const weight = extractWeight(text);
            
            console.log('handleInvoiceImageUpload: Extraction completed');
            console.log('handleInvoiceImageUpload: Customer name result:', customerName);
            console.log('handleInvoiceImageUpload: Weight result:', weight);
            
            // Fill the form fields
            const clientInput = document.getElementById('clientNameInput');
            const weightInput = document.getElementById('weightInput');
            
            console.log('handleInvoiceImageUpload: Form inputs found - clientInput:', !!clientInput, 'weightInput:', !!weightInput);
            
            let successCount = 0;
            let message = '';
            
            if (customerName && clientInput) {
                clientInput.value = customerName;
                // Trigger input event to ensure the field updates properly
                clientInput.dispatchEvent(new Event('input', { bubbles: true }));
                clientInput.dispatchEvent(new Event('change', { bubbles: true }));
                successCount++;
                message += `✓ Nom client: ${customerName}\n`;
                console.log('handleInvoiceImageUpload: Customer name set to:', customerName);
                console.log('handleInvoiceImageUpload: Customer name input value after setting:', clientInput.value);
            } else {
                message += `⚠ Nom client non trouvé\n`;
                if (!customerName) {
                    console.warn('handleInvoiceImageUpload: Customer name not found');
                }
                if (!clientInput) {
                    console.warn('handleInvoiceImageUpload: Customer name input field not found (id="clientNameInput")');
                }
            }
            
            if (weight && weightInput) {
                weightInput.value = weight;
                // Trigger input event to ensure the field updates properly
                weightInput.dispatchEvent(new Event('input', { bubbles: true }));
                weightInput.dispatchEvent(new Event('change', { bubbles: true }));
                successCount++;
                message += `✓ Poids: ${weight} kg\n`;
                console.log('handleInvoiceImageUpload: Weight set to:', weight);
                console.log('handleInvoiceImageUpload: Weight input value after setting:', weightInput.value);
            } else {
                message += `⚠ Poids non trouvé\n`;
                if (!weight) {
                    console.warn('handleInvoiceImageUpload: Weight not found');
                }
                if (!weightInput) {
                    console.warn('handleInvoiceImageUpload: Weight input field not found (id="weightInput")');
                }
            }
            
            // Hide progress indicator
            if (progressDiv) {
                progressDiv.style.display = 'none';
            }
            
            // Show result message
            if (successCount > 0) {
                showToast(`Données extraites: ${successCount}/2`, 'success');
            } else {
                console.warn('handleInvoiceImageUpload: No data extracted. Full OCR text:', text);
                showToast('Aucune donnée extraite. Vérifiez que la photo est claire. Consultez la console pour plus de détails.', 'warning');
            }
            
        } catch (err) {
            console.error('handleInvoiceImageUpload: OCR Error:', err);
            console.error('handleInvoiceImageUpload: Error stack:', err.stack);
            if (progressDiv) {
                progressDiv.style.display = 'none';
            }
            showToast('Erreur lors de l\'analyse de la facture. Consultez la console pour plus de détails.', 'error');
        }
    };
    
    reader.readAsDataURL(file);
}
