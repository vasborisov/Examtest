// Restaurant Reservation System JavaScript

// Main initialization
document.addEventListener('DOMContentLoaded', function() {
    // Auto-submit form when sort dropdown changes
    const sortSelect = document.querySelector('select[name="sortBy"]');
    if (sortSelect) {
        sortSelect.addEventListener('change', function() {
            this.form.submit();
        });
    }

    // Initialize search suggestions
    initializeSearchSuggestions();
    
    // Initialize tooltips if Bootstrap is available
    if (typeof bootstrap !== 'undefined') {
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    // Initialize form validation enhancements
    initializeFormValidation();
    
    // Initialize reservation form validation
    initializeReservationValidation();
});

// Search suggestions functionality
function initializeSearchSuggestions() {
    const searchInput = document.getElementById('searchInput');
    if (!searchInput) return;

    let suggestionBox = null;
    let currentFocus = -1;

    // Create suggestion box
    function createSuggestionBox() {
        suggestionBox = document.createElement('div');
        suggestionBox.className = 'search-suggestions';
        suggestionBox.style.cssText = `
            position: absolute;
            top: 100%;
            left: 0;
            right: 0;
            background: white;
            border: 1px solid #ddd;
            border-top: none;
            max-height: 300px;
            overflow-y: auto;
            z-index: 1000;
            border-radius: 0 0 0.375rem 0.375rem;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
        `;
        searchInput.parentNode.style.position = 'relative';
        searchInput.parentNode.appendChild(suggestionBox);
    }

    // Search with debounce
    let searchTimeout;
    searchInput.addEventListener('input', function() {
        clearTimeout(searchTimeout);
        const term = this.value.trim();
        
        if (term.length < 2) {
            hideSuggestions();
            return;
        }

        searchTimeout = setTimeout(() => {
            fetchSuggestions(term);
        }, 300);
    });

    // Fetch suggestions from server
    async function fetchSuggestions(term) {
        try {
            const response = await fetch(`/Restaurants/SearchSuggestions?term=${encodeURIComponent(term)}`);
            const suggestions = await response.json();
            showSuggestions(suggestions);
        } catch (error) {
            console.error('Error fetching suggestions:', error);
        }
    }

    // Display suggestions
    function showSuggestions(suggestions) {
        if (!suggestionBox) {
            createSuggestionBox();
        }

        if (suggestions.length === 0) {
            hideSuggestions();
            return;
        }

        suggestionBox.innerHTML = suggestions.map((item, index) => `
            <div class="suggestion-item" data-index="${index}" data-id="${item.id}" style="
                padding: 12px 16px;
                border-bottom: 1px solid #eee;
                cursor: pointer;
                transition: background-color 0.2s;
            " onmouseover="this.style.backgroundColor='#f8f9fa'" onmouseout="this.style.backgroundColor='white'">
                <div style="font-weight: 500; color: #212529;">${item.title}</div>
                <div style="font-size: 0.875rem; color: #6c757d; margin-top: 2px;">
                    <i class="fas fa-map-marker-alt"></i> ${item.address}
                </div>
                <div style="font-size: 0.75rem; color: #adb5bd; margin-top: 2px;">${item.description}</div>
            </div>
        `).join('');

        suggestionBox.style.display = 'block';
        currentFocus = -1;

        // Add click handlers
        suggestionBox.querySelectorAll('.suggestion-item').forEach(item => {
            item.addEventListener('click', function() {
                const restaurantId = this.getAttribute('data-id');
                window.location.href = `/Restaurants/Details/${restaurantId}`;
            });
        });
    }

    // Hide suggestions
    function hideSuggestions() {
        if (suggestionBox) {
            suggestionBox.style.display = 'none';
        }
    }

    // Keyboard navigation
    searchInput.addEventListener('keydown', function(e) {
        if (!suggestionBox || suggestionBox.style.display === 'none') return;

        const items = suggestionBox.querySelectorAll('.suggestion-item');
        
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            currentFocus++;
            if (currentFocus >= items.length) currentFocus = 0;
            setActive(items);
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            currentFocus--;
            if (currentFocus < 0) currentFocus = items.length - 1;
            setActive(items);
        } else if (e.key === 'Enter' && currentFocus > -1) {
            e.preventDefault();
            items[currentFocus].click();
        } else if (e.key === 'Escape') {
            hideSuggestions();
        }
    });

    // Set active suggestion
    function setActive(items) {
        items.forEach((item, index) => {
            if (index === currentFocus) {
                item.style.backgroundColor = '#e9ecef';
            } else {
                item.style.backgroundColor = 'white';
            }
        });
    }

    // Hide suggestions when clicking outside
    document.addEventListener('click', function(e) {
        if (!searchInput.contains(e.target) && !suggestionBox?.contains(e.target)) {
            hideSuggestions();
        }
    });
}

// Form validation enhancements
function initializeFormValidation() {
    // Real-time validation for email fields
    const emailInputs = document.querySelectorAll('input[type="email"]');
    emailInputs.forEach(input => {
        input.addEventListener('blur', function() {
            validateEmail(this);
        });
    });

    // Real-time validation for password fields
    const passwordInputs = document.querySelectorAll('input[type="password"]');
    passwordInputs.forEach(input => {
        input.addEventListener('input', function() {
            validatePassword(this);
        });
    });

    // Form submission validation
    const forms = document.querySelectorAll('form');
    forms.forEach(form => {
        form.addEventListener('submit', function(e) {
            if (!validateForm(this)) {
                e.preventDefault();
                showToast('Please correct the errors in the form.', 'error');
                return false;
            }

            // Add loading state to submit button
            const submitBtn = this.querySelector('button[type="submit"]');
            if (submitBtn && !submitBtn.disabled) {
                submitBtn.classList.add('btn-loading');
                submitBtn.disabled = true;
                
                // Prevent double-submission
                setTimeout(() => {
                    if (submitBtn) {
                        submitBtn.classList.remove('btn-loading');
                        submitBtn.disabled = false;
                    }
                }, 5000);
            }
        });
    });
}

// Reservation form specific validation
function initializeReservationValidation() {
    const reservationForm = document.querySelector('form[action*="MakeReservation"]');
    if (!reservationForm) return;

    const dateTimeInput = reservationForm.querySelector('input[name="reservationTime"]');
    const peopleInput = reservationForm.querySelector('select[name="peopleCount"], input[name="peopleCount"]');
    
    if (dateTimeInput) {
        dateTimeInput.addEventListener('change', function() {
            validateReservationDateTime(this);
        });
        
        // Set minimum date/time to 30 minutes from now
        const now = new Date();
        now.setMinutes(now.getMinutes() + 30);
        const minDateTime = now.toISOString().slice(0, 16);
        dateTimeInput.min = minDateTime;
    }

    if (peopleInput) {
        peopleInput.addEventListener('change', function() {
            validatePeopleCount(this);
        });
    }
}

// Validation functions
function validateEmail(input) {
    const email = input.value.trim();
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    
    clearFieldValidation(input);
    
    if (email && !emailRegex.test(email)) {
        showFieldError(input, 'Please enter a valid email address.');
        return false;
    }
    
    return true;
}

function validatePassword(input) {
    const password = input.value;
    const errors = [];
    
    if (password.length > 0) {
        if (password.length < 6) {
            errors.push('Password must be at least 6 characters long.');
        }
        if (!/\d/.test(password)) {
            errors.push('Password must contain at least one number.');
        }
        if (!/[a-zA-Z]/.test(password)) {
            errors.push('Password must contain at least one letter.');
        }
    }
    
    clearFieldValidation(input);
    
    if (errors.length > 0) {
        showFieldError(input, errors.join(' '));
        return false;
    }
    
    return true;
}

function validateReservationDateTime(input) {
    const selectedDate = new Date(input.value);
    const now = new Date();
    const minDate = new Date(now.getTime() + 30 * 60000); // 30 minutes from now
    const maxDate = new Date(now.getTime() + 365 * 24 * 60 * 60000); // 1 year from now
    
    clearFieldValidation(input);
    
    if (selectedDate < minDate) {
        showFieldError(input, 'Reservation must be at least 30 minutes in the future.');
        return false;
    }
    
    if (selectedDate > maxDate) {
        showFieldError(input, 'Reservations can only be made up to 1 year in advance.');
        return false;
    }
    
    // Check business hours (9 AM to 11 PM)
    const hour = selectedDate.getHours();
    if (hour < 9 || hour > 23) {
        showFieldError(input, 'Reservations are only available between 9:00 AM and 11:00 PM.');
        return false;
    }
    
    return true;
}

function validatePeopleCount(input) {
    const count = parseInt(input.value);
    
    clearFieldValidation(input);
    
    if (count < 1) {
        showFieldError(input, 'Number of people must be at least 1.');
        return false;
    }
    
    if (count > 20) {
        showFieldError(input, 'Maximum 20 people per reservation.');
        return false;
    }
    
    return true;
}

function validateForm(form) {
    let isValid = true;
    
    // Validate all required fields
    const requiredFields = form.querySelectorAll('[required]');
    requiredFields.forEach(field => {
        if (!field.value.trim()) {
            showFieldError(field, 'This field is required.');
            isValid = false;
        }
    });
    
    // Validate email fields
    const emailFields = form.querySelectorAll('input[type="email"]');
    emailFields.forEach(field => {
        if (!validateEmail(field)) {
            isValid = false;
        }
    });
    
    // Validate password fields
    const passwordFields = form.querySelectorAll('input[type="password"]');
    passwordFields.forEach(field => {
        if (field.value && !validatePassword(field)) {
            isValid = false;
        }
    });
    
    return isValid;
}

function showFieldError(input, message) {
    input.classList.add('is-invalid');
    
    // Remove existing error message
    const existingError = input.parentNode.querySelector('.invalid-feedback');
    if (existingError) {
        existingError.remove();
    }
    
    // Add new error message
    const errorDiv = document.createElement('div');
    errorDiv.className = 'invalid-feedback';
    errorDiv.textContent = message;
    input.parentNode.appendChild(errorDiv);
}

function clearFieldValidation(input) {
    input.classList.remove('is-invalid');
    const errorMessage = input.parentNode.querySelector('.invalid-feedback');
    if (errorMessage) {
        errorMessage.remove();
    }
}

// Utility functions
function showToast(message, type = 'success') {
    // Create toast container if it doesn't exist
    let toastContainer = document.querySelector('.toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
        toastContainer.style.zIndex = '1055';
        document.body.appendChild(toastContainer);
    }

    // Create toast
    const toastId = 'toast-' + Date.now();
    const toastHTML = `
        <div id="${toastId}" class="toast align-items-center text-white bg-${type === 'success' ? 'success' : 'danger'} border-0" role="alert">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-triangle'}"></i> ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;
    
    toastContainer.insertAdjacentHTML('beforeend', toastHTML);
    
    // Initialize and show toast
    const toastElement = document.getElementById(toastId);
    if (typeof bootstrap !== 'undefined') {
        const toast = new bootstrap.Toast(toastElement, { delay: 5000 });
        toast.show();
        
        // Remove toast element after it's hidden
        toastElement.addEventListener('hidden.bs.toast', function() {
            this.remove();
        });
    } else {
        // Fallback if Bootstrap is not available
        setTimeout(() => {
            toastElement.remove();
        }, 5000);
    }
}
