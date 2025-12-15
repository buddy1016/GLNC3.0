let calendar;
let currentView = 'month';
let currentDate = new Date();

// Initialize the planning calendar
function initPlanningCalendar() {
    const calendarEl = document.getElementById('calendar');
    
    calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'timeGridWeek',
        locale: 'fr',
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
                    
                    // Insert image before the time element
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
            
            // Set driver, truck, and dates (disabled)
            const driverSelect = document.getElementById('modalDriver');
            const truckSelect = document.getElementById('modalTruck');
            const appointmentInput = document.getElementById('modalAppointment');
            const leaveInput = document.getElementById('modalLeave');
            
            driverSelect.value = delivery.userId;
            driverSelect.disabled = true;
            truckSelect.value = delivery.truckId;
            truckSelect.disabled = true;
            appointmentInput.value = formatDateTimeLocal(new Date(delivery.dateTimeAppointment));
            appointmentInput.disabled = true;
            leaveInput.value = formatDateTimeLocal(new Date(delivery.dateTimeLeave));
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

    $.ajax({
        url: '/Planning/Create',
        type: 'POST',
        data: data,
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        success: function(response) {
            if (response.success) {
                showToast('Delivery saved successfully', 'success');
                bootstrap.Modal.getInstance(document.getElementById('deliveryModal')).hide();
                refreshCalendar();
            } else {
                let errorMsg = response.message || 'Error saving delivery';
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
            
            // Update only the time fields
            delivery.dateTimeAppointment = start.toISOString();
            delivery.dateTimeLeave = end.toISOString();
            
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
function formatDateTimeLocal(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
}

