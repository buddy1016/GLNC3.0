// Reusable Pagination Script
(function($) {
    'use strict';

    // Initialize pagination for a table
    function initPagination(tableSelector, pageSize = 10) {
        const $table = $(tableSelector);
        if ($table.length === 0) {
            console.warn('Table not found:', tableSelector);
            return;
        }
        
        const $tbody = $table.find('tbody');
        const $rows = $tbody.find('tr');
        const totalRows = $rows.length;
        
        if (totalRows === 0) {
            return; // No rows to paginate
        }
        
        const totalPages = Math.ceil(totalRows / pageSize);
        
        // Store pagination state
        let currentPage = 1;
        const tableId = $table.attr('id') || 'table_' + Math.random().toString(36).substr(2, 9);
        const paginationId = tableId + '_pagination';
        
        // Remove existing pagination if any
        $('#' + paginationId).remove();
        
        // Create pagination container
        const $paginationContainer = $('<div>', {
            id: paginationId,
            class: 'pagination-container d-flex justify-content-between align-items-center mt-3'
        });
        
        // Page size selector
        const $pageSizeSelect = $('<div>', {
            class: 'd-flex align-items-center gap-2'
        }).html(`
            <label class="mb-0">Show:</label>
            <select class="form-select form-select-sm page-size-select" style="width: auto;">
                <option value="10" ${pageSize === 10 ? 'selected' : ''}>10</option>
                <option value="20" ${pageSize === 20 ? 'selected' : ''}>20</option>
                <option value="50" ${pageSize === 50 ? 'selected' : ''}>50</option>
                <option value="100" ${pageSize === 100 ? 'selected' : ''}>100</option>
            </select>
            <span class="text-muted">entries</span>
        `);
        
        // Pagination controls
        const $paginationControls = $('<div>', {
            class: 'd-flex align-items-center gap-2'
        });
        
        const $info = $('<span>', {
            class: 'text-muted me-3'
        });
        
        const $firstBtn = $('<button>', {
            class: 'btn btn-sm btn-outline-secondary',
            html: '<i class="bi bi-chevron-double-left"></i>',
            title: 'First'
        });
        
        const $prevBtn = $('<button>', {
            class: 'btn btn-sm btn-outline-secondary',
            html: '<i class="bi bi-chevron-left"></i>',
            title: 'Previous'
        });
        
        const $pageInput = $('<input>', {
            type: 'number',
            class: 'form-control form-control-sm',
            style: 'width: 60px; text-align: center;',
            min: 1,
            max: totalPages
        });
        
        const $ofPages = $('<span>', {
            class: 'text-muted'
        });
        
        const $nextBtn = $('<button>', {
            class: 'btn btn-sm btn-outline-secondary',
            html: '<i class="bi bi-chevron-right"></i>',
            title: 'Next'
        });
        
        const $lastBtn = $('<button>', {
            class: 'btn btn-sm btn-outline-secondary',
            html: '<i class="bi bi-chevron-double-right"></i>',
            title: 'Last'
        });
        
        $paginationControls.append($info, $firstBtn, $prevBtn, $pageInput, $ofPages, $nextBtn, $lastBtn);
        $paginationContainer.append($pageSizeSelect, $paginationControls);
        
        // Insert pagination after table
        $table.after($paginationContainer);
        
        // Function to show page
        function showPage(page) {
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;
            
            currentPage = page;
            const start = (page - 1) * pageSize;
            const end = start + pageSize;
            
            $rows.hide();
            $rows.slice(start, end).show();
            
            // Update pagination UI
            updatePaginationUI();
        }
        
        // Update pagination UI
        function updatePaginationUI() {
            const start = totalRows === 0 ? 0 : (currentPage - 1) * pageSize + 1;
            const end = Math.min(currentPage * pageSize, totalRows);
            
            $info.text(`Showing ${start} to ${end} of ${totalRows} entries`);
            $pageInput.val(currentPage);
            $ofPages.text(`of ${totalPages}`);
            
            // Update button states
            $firstBtn.prop('disabled', currentPage === 1 || totalPages === 0);
            $prevBtn.prop('disabled', currentPage === 1 || totalPages === 0);
            $nextBtn.prop('disabled', currentPage === totalPages || totalPages === 0);
            $lastBtn.prop('disabled', currentPage === totalPages || totalPages === 0);
        }
        
        // Event handlers
        $firstBtn.on('click', function() {
            showPage(1);
        });
        
        $prevBtn.on('click', function() {
            showPage(currentPage - 1);
        });
        
        $nextBtn.on('click', function() {
            showPage(currentPage + 1);
        });
        
        $lastBtn.on('click', function() {
            showPage(totalPages);
        });
        
        $pageInput.on('change', function() {
            const page = parseInt($(this).val());
            if (!isNaN(page) && page >= 1 && page <= totalPages) {
                showPage(page);
            } else {
                $(this).val(currentPage);
            }
        });
        
        $pageSizeSelect.find('.page-size-select').on('change', function() {
            pageSize = parseInt($(this).val());
            currentPage = 1;
            const newTotalPages = Math.ceil(totalRows / pageSize);
            $pageInput.attr('max', newTotalPages);
            showPage(1);
        });
        
        // Initialize
        showPage(1);
    }
    
    // Make it available globally
    window.initPagination = initPagination;
    
})(jQuery);

