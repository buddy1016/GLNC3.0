package com.example.glnc;

import android.Manifest;
import android.content.Context;
import android.content.pm.PackageManager;
import android.location.Criteria;
import android.location.LocationListener;
import android.location.LocationManager;
import android.location.LocationProvider;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.util.Log;

import androidx.core.app.ActivityCompat;

/**
 * Continuous GPS Location Tracking Module
 * Based on Worktime-Famoco GPS Location Tracking Documentation
 * Uses Android's native LocationManager API (no Google Play Services required)
 * Provides continuous location updates (3 minutes / 30 meters threshold)
 * Location available via direct field access: location.latitude, location.longitude
 * Stores location in Global class for unified access
 */
public class Location {
    private final Context context;
    private LocationManager locationManager = null;
    private String provider;
    public double latitude;
    public double longitude;
    public double altitude;
    private LocationListener listenerGPS;
    
    // Track mock location rejections for automatic fallback to network
    private int gpsMockRejectionCount = 0;
    private static final int MAX_GPS_MOCK_REJECTIONS = 3; // Switch to network after 3 rejections
    private boolean hasSwitchedToNetwork = false; // Track if we've already switched
    
    // Timeout mechanism: Switch to network if no location received within timeout
    private Handler timeoutHandler;
    private Runnable timeoutRunnable;
    private static final long GPS_TIMEOUT_MS = 30000; // 30 seconds timeout for GPS
    private boolean hasValidLocation = false; // Track if we've received a valid location

    public Location(Context context) {
        this.context = context;
        this.latitude = 0.0;
        this.longitude = 0.0;
        this.altitude = 0.0;
    }

    /**
     * Check if location is from mock provider (test/emulator location)
     * Prevents using fake locations like Washington DC when in Tokyo/New Caledonia
     * Enhanced detection for Famoco devices and mock location apps
     */
    private boolean isMockLocation(android.location.Location location) {
        if (location == null) {
            return false;
        }
        
        double lat = location.getLatitude();
        double lon = location.getLongitude();
        
        // Method 1: Direct Android API check (most reliable)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.JELLY_BEAN_MR2) {
            try {
                if (location.isFromMockProvider()) {
                    Log.e("Location", "REJECTED: Location is from MOCK provider - " + lat + ", " + lon);
                    return true;
                }
            } catch (Exception e) {
                Log.w("Location", "Error checking mock provider", e);
            }
        }
        
        // Method 2: Check for suspicious accuracy patterns
        // Real GPS accuracy is typically 5-20m and varies. Exactly 1.0m is suspicious (common in mock locations)
        if (location.hasAccuracy()) {
            float accuracy = location.getAccuracy();
            // Accuracy of exactly 1.0m is a red flag (LDPlayer/emulators often use this)
            if (accuracy == 1.0f) {
                Log.w("Location", "Suspicious accuracy: exactly 1.0m (common in mock locations)");
                // If combined with known mock coordinates, definitely reject
                if ((Math.abs(lat - 38.907) < 0.01 && Math.abs(lon - (-77.036)) < 0.01)) {
                    Log.e("Location", "REJECTED: Washington DC coordinates with suspicious 1.0m accuracy - " + lat + ", " + lon);
                    return true;
                }
            }
            // Accuracy < 0.1m is almost certainly mock
            if (accuracy < 0.1f) {
                Log.e("Location", "REJECTED: Absurdly precise accuracy: " + accuracy + "m");
                return true;
            }
        }
        
        // Method 3: Check for known mock/test location coordinates (LDPlayer default)
        // Washington DC: 38.907, -77.036 (LDPlayer's default location)
        // Only reject if accuracy is also suspicious (to avoid blocking real users in DC)
        if ((Math.abs(lat - 38.907) < 0.01 && Math.abs(lon - (-77.036)) < 0.01)) {
            // Washington DC coordinates - check if accuracy is suspicious
            if (location.hasAccuracy() && location.getAccuracy() <= 1.0f) {
                Log.e("Location", "REJECTED: Washington DC coordinates with suspicious accuracy - " + lat + ", " + lon + " (accuracy: " + location.getAccuracy() + "m)");
                return true;
            }
        }
        
        // Method 4: Check if mock locations are enabled in Developer Options
        try {
            if (android.provider.Settings.Secure.getInt(
                    context.getContentResolver(),
                    android.provider.Settings.Secure.ALLOW_MOCK_LOCATION
            ) == 1) {
                // Mock locations are enabled - be more cautious
                // Check for location age - very old locations might be cached mock
                long age = System.currentTimeMillis() - location.getTime();
                if (age > 86400000) { // Older than 24 hours
                    Log.w("Location", "Possible mock: Very old location: " + (age / 3600000) + " hours");
                    // Only reject if also has suspicious accuracy
                    if (location.hasAccuracy() && location.getAccuracy() <= 1.0f) {
                        Log.e("Location", "REJECTED: Very old location with suspicious accuracy");
                        return true;
                    }
                }
            }
        } catch (Exception e) {
            Log.w("Location", "Could not check mock location setting", e);
        }
        
        return false;
    }

    /**
     * Initialize location tracking and start continuous updates
     * Matches Worktime-Famoco documentation implementation
     * Updates occur every 3 minutes OR when device moves 30+ meters
     */
    public void initLocation() {
        // Clear any existing mock locations from storage on startup
        clearMockLocationFromStorage();
        
        // Reset mock rejection counter when initializing
        gpsMockRejectionCount = 0;
        hasSwitchedToNetwork = false;
        hasValidLocation = false;
        
        // Initialize timeout handler
        if (timeoutHandler == null) {
            timeoutHandler = new Handler(Looper.getMainLooper());
        }
        
        // Get LocationManager service
        if (locationManager == null) {
            locationManager = (LocationManager) context.getApplicationContext()
                .getSystemService(Context.LOCATION_SERVICE);
            
            // Auto-select best provider: GPS preferred, network fallback
            // Step 1: Try GPS_PROVIDER first (highest accuracy)
            if (locationManager.isProviderEnabled(LocationManager.GPS_PROVIDER)) {
                provider = LocationManager.GPS_PROVIDER;
                Log.d("Location", "Using GPS_PROVIDER (preferred for high accuracy)");
            } else {
                // Step 2: GPS not available, use Criteria to auto-select best available provider
                Criteria criteres = new Criteria();
                
                // Accuracy: High precision required
                criteres.setAccuracy(Criteria.ACCURACY_FINE);
                
                // Altitude: Not required
                criteres.setAltitudeRequired(false);
                
                // Bearing (direction): Not required
                criteres.setBearingRequired(false);
                
                // Speed: Not required
                criteres.setSpeedRequired(false);
                
                // Cost: Network usage allowed (for network provider fallback)
                criteres.setCostAllowed(true);
                
                // Power: Medium power consumption (balance between accuracy and battery)
                criteres.setPowerRequirement(Criteria.POWER_MEDIUM);
                
                // Auto-select best available provider (network, passive, etc.)
                provider = locationManager.getBestProvider(criteres, true);
                Log.d("Location", "GPS not available, auto-selected provider: " + provider + " (network fallback)");
            }
        }
        
        // Check permissions
        if (provider != null) {
            if (ActivityCompat.checkSelfPermission(context.getApplicationContext(), 
                    Manifest.permission.ACCESS_FINE_LOCATION) != PackageManager.PERMISSION_GRANTED 
                && 
                ActivityCompat.checkSelfPermission(context.getApplicationContext(), 
                    Manifest.permission.ACCESS_COARSE_LOCATION) != PackageManager.PERMISSION_GRANTED) {
                Log.d("Location", "No location permissions granted");
                return;
            }
            
            // Remove existing listener if any (prevent duplicates)
            if (listenerGPS != null && locationManager != null) {
                try {
                    locationManager.removeUpdates(listenerGPS);
                } catch (Exception e) {
                    Log.w("Location", "Error removing existing listener", e);
                }
            }
            
            // Create LocationListener
            listenerGPS = new LocationListener() {
                @Override
                public void onLocationChanged(android.location.Location location) {
                    // Log incoming location for debugging
                    Log.d("Location", "onLocationChanged received: " + location.getLatitude() + ", " + 
                          location.getLongitude() + " (provider: " + location.getProvider() + 
                          ", accuracy: " + (location.hasAccuracy() ? location.getAccuracy() + "m" : "unknown") + ")");
                    
                    // Reject mock locations
                    if (isMockLocation(location)) {
                        Log.e("Location", "REJECTED: Mock location detected - " + 
                              location.getLatitude() + ", " + location.getLongitude());
                        
                        // If GPS is returning mock locations, count rejections and switch to network
                        if (LocationManager.GPS_PROVIDER.equals(provider) && !hasSwitchedToNetwork) {
                            gpsMockRejectionCount++;
                            Log.w("Location", "GPS mock rejection count: " + gpsMockRejectionCount + "/" + MAX_GPS_MOCK_REJECTIONS);
                            
                            // Switch to network provider after too many mock rejections
                            if (gpsMockRejectionCount >= MAX_GPS_MOCK_REJECTIONS) {
                                Log.w("Location", "GPS consistently returning mock locations. Switching to NETWORK_PROVIDER...");
                                switchToNetworkProvider();
                            }
                        }
                        return;
                    }
                    
                    // Valid location received - cancel timeout and reset counters
                    cancelGpsTimeout();
                    hasValidLocation = true;
                    
                    if (gpsMockRejectionCount > 0) {
                        Log.d("Location", "Valid location received, resetting mock rejection counter");
                        gpsMockRejectionCount = 0;
                    }
                    
                    // Update direct fields for easy access
                    latitude = location.getLatitude();
                    longitude = location.getLongitude();
                    altitude = location.getAltitude();
                    
                    Log.d("Location", "Location ACCEPTED and updated: " + latitude + ", " + longitude + 
                          " (accuracy: " + (location.hasAccuracy() ? location.getAccuracy() + "m" : "unknown") + 
                          ", provider: " + location.getProvider() + ")");
                    
                    // Store location in Global class for unified access
                    Global.setLocation(location);
                    
                    // Also store in SharedPreferences for persistence
                    storeLocation(location);
                }

                @Override
                public void onProviderDisabled(String fournisseur) {
                    Log.d("Location", "Provider disabled: " + fournisseur);
                }

                @Override
                public void onProviderEnabled(String fournisseur) {
                    Log.d("Location", "Provider enabled: " + fournisseur);
                }

                @Override
                public void onStatusChanged(String fournisseur, int status, Bundle extras) {
                    switch (status) {
                        case LocationProvider.AVAILABLE:
                            Log.d("Location", "Provider available: " + fournisseur);
                            break;
                        case LocationProvider.OUT_OF_SERVICE:
                            Log.w("Location", "Provider out of service: " + fournisseur);
                            break;
                        case LocationProvider.TEMPORARILY_UNAVAILABLE:
                            Log.w("Location", "Provider temporarily unavailable: " + fournisseur);
                            break;
                    }
                }
            };
            
            // Phase 4: Get cached location immediately (as per documentation)
            // BUT: Check if cached location is mock before using it
            android.location.Location cachedLocation = locationManager.getLastKnownLocation(provider);
            if (cachedLocation != null) {
                if (isMockLocation(cachedLocation)) {
                    Log.e("Location", "REJECTED cached location - detected as mock: " + 
                          cachedLocation.getLatitude() + ", " + cachedLocation.getLongitude());
                    // Clear the mock location from SharedPreferences if it exists
                    clearMockLocationFromStorage();
                    Log.d("Location", "No valid cached location available (mock detected), waiting for fresh GPS fix");
                } else {
                    // Immediately notify listener with cached location
                    Log.d("Location", "Using cached location: " + cachedLocation.getLatitude() + ", " + cachedLocation.getLongitude());
                    hasValidLocation = true; // Mark as valid so timeout won't trigger
                    listenerGPS.onLocationChanged(cachedLocation);
                }
            } else {
                Log.d("Location", "No cached location available, waiting for fresh GPS fix");
            }
            
            // Phase 5: Request continuous location updates
            // Updates occur when EITHER condition is met:
            // - Time elapsed: 3 minutes (180,000 ms)
            // - Distance moved: 30 meters
            locationManager.requestLocationUpdates(
                provider,              // Selected provider (gps/network/passive)
                3 * 60 * 1000,        // minTime: 3 minutes (180,000 ms)
                30,                    // minDistance: 30 meters
                listenerGPS            // LocationListener callback
            );
            
            Log.d("Location", "Location updates requested - Updates every 3 min or 30m movement");
            
            // Start timeout: If GPS is used and no valid location yet, switch to network after timeout
            if (LocationManager.GPS_PROVIDER.equals(provider) && !hasValidLocation && !hasSwitchedToNetwork) {
                startGpsTimeout();
            }
        } else {
            Log.e("Location", "No location provider available!");
        }
    }

    /**
     * Stop location updates and clean up resources
     * Should be called in Activity's onPause() method
     */
    public void stopLocation() {
        // Cancel timeout
        cancelGpsTimeout();
        
        if (locationManager != null && listenerGPS != null) {
            try {
                locationManager.removeUpdates(listenerGPS);
                listenerGPS = null;
                Log.d("Location", "Location updates stopped");
            } catch (Exception e) {
                Log.e("Location", "Error stopping location updates", e);
            }
        }
    }
    
    /**
     * Start GPS timeout - switch to network if no location received within timeout period
     */
    private void startGpsTimeout() {
        // Cancel any existing timeout
        cancelGpsTimeout();
        
        if (timeoutHandler == null) {
            timeoutHandler = new Handler(Looper.getMainLooper());
        }
        
        timeoutRunnable = new Runnable() {
            @Override
            public void run() {
                // Timeout expired - no location received from GPS
                if (!hasValidLocation && LocationManager.GPS_PROVIDER.equals(provider) && !hasSwitchedToNetwork) {
                    Log.w("Location", "GPS timeout (" + (GPS_TIMEOUT_MS / 1000) + "s) - no location received. Switching to NETWORK_PROVIDER...");
                    switchToNetworkProvider();
                }
            }
        };
        
        timeoutHandler.postDelayed(timeoutRunnable, GPS_TIMEOUT_MS);
        Log.d("Location", "GPS timeout started: Will switch to network if no location received within " + (GPS_TIMEOUT_MS / 1000) + " seconds");
    }
    
    /**
     * Cancel GPS timeout
     */
    private void cancelGpsTimeout() {
        if (timeoutHandler != null && timeoutRunnable != null) {
            timeoutHandler.removeCallbacks(timeoutRunnable);
            timeoutRunnable = null;
        }
    }
    
    /**
     * Switch from GPS to network provider when GPS consistently returns mock locations
     * This allows the app to get real location even when GPS is compromised
     */
    private void switchToNetworkProvider() {
        if (locationManager == null || listenerGPS == null) {
            return;
        }
        
        // Check if network provider is available
        if (!locationManager.isProviderEnabled(LocationManager.NETWORK_PROVIDER)) {
            Log.e("Location", "Cannot switch to network provider - not enabled");
            return;
        }
        
        // Check permissions
        if (ActivityCompat.checkSelfPermission(context.getApplicationContext(), 
                Manifest.permission.ACCESS_FINE_LOCATION) != PackageManager.PERMISSION_GRANTED 
            && 
            ActivityCompat.checkSelfPermission(context.getApplicationContext(), 
                Manifest.permission.ACCESS_COARSE_LOCATION) != PackageManager.PERMISSION_GRANTED) {
            Log.e("Location", "Cannot switch to network provider - no permissions");
            return;
        }
        
        try {
            // Remove current GPS updates
            locationManager.removeUpdates(listenerGPS);
            Log.d("Location", "Removed GPS location updates");
            
            // Cancel GPS timeout
            cancelGpsTimeout();
            
            // Switch to network provider
            provider = LocationManager.NETWORK_PROVIDER;
            hasSwitchedToNetwork = true;
            gpsMockRejectionCount = 0; // Reset counter
            
            Log.w("Location", "Switched to NETWORK_PROVIDER (GPS timeout or mock locations detected)");
            
            // Get cached network location if available
            android.location.Location cachedLocation = locationManager.getLastKnownLocation(LocationManager.NETWORK_PROVIDER);
            if (cachedLocation != null && !isMockLocation(cachedLocation)) {
                Log.d("Location", "Using cached network location: " + cachedLocation.getLatitude() + ", " + cachedLocation.getLongitude());
                listenerGPS.onLocationChanged(cachedLocation);
            }
            
            // Request continuous location updates from network provider
            locationManager.requestLocationUpdates(
                LocationManager.NETWORK_PROVIDER,  // Network provider
                3 * 60 * 1000,                     // minTime: 3 minutes
                30,                                // minDistance: 30 meters
                listenerGPS                        // Same listener
            );
            
            Log.d("Location", "Network location updates requested - Updates every 3 min or 30m movement");
        } catch (Exception e) {
            Log.e("Location", "Error switching to network provider", e);
        }
    }

    /**
     * Store location in SharedPreferences for persistence
     * Never stores mock locations
     */
    private void storeLocation(android.location.Location location) {
        try {
            // Don't store mock locations
            if (isMockLocation(location)) {
                Log.e("Location", "NOT storing mock location: " + location.getLatitude() + ", " + location.getLongitude());
                return;
            }
            
            android.content.SharedPreferences prefs = 
                context.getSharedPreferences("GLNC_Prefs", Context.MODE_PRIVATE);
            android.content.SharedPreferences.Editor editor = prefs.edit();
            editor.putFloat("latitude", (float) location.getLatitude());
            editor.putFloat("longitude", (float) location.getLongitude());
            editor.putFloat("altitude", (float) location.getAltitude());
            editor.putLong("location_timestamp", location.getTime());
            if (location.hasAccuracy()) {
                editor.putFloat("location_accuracy", location.getAccuracy());
            }
            editor.apply();
            Log.d("Location", "Location stored in SharedPreferences: " + location.getLatitude() + ", " + location.getLongitude());
        } catch (Exception e) {
            Log.e("Location", "Error storing location", e);
        }
    }
    
    /**
     * Clear mock location from SharedPreferences and Global storage if it exists
     * Called when a mock location is detected or on app startup
     */
    private void clearMockLocationFromStorage() {
        try {
            // Check SharedPreferences
            android.content.SharedPreferences prefs = 
                context.getSharedPreferences("GLNC_Prefs", Context.MODE_PRIVATE);
            float lat = prefs.getFloat("latitude", 0.0f);
            float lon = prefs.getFloat("longitude", 0.0f);
            
            boolean isMock = false;
            
            // Check if stored location matches known mock coordinates
            if ((Math.abs(lat - 38.907f) < 0.01f && Math.abs(lon - (-77.036f)) < 0.01f) ||
                (Math.abs(lat - 37.7749f) < 0.01f && Math.abs(lon - (-122.4194f)) < 0.01f) ||
                (Math.abs(lat - 40.7128f) < 0.01f && Math.abs(lon - (-74.0060f)) < 0.01f)) {
                isMock = true;
            }
            
            // Also check Global storage
            android.location.Location globalLocation = Global.getLocation();
            if (globalLocation != null) {
                double globalLat = globalLocation.getLatitude();
                double globalLon = globalLocation.getLongitude();
                if ((Math.abs(globalLat - 38.907) < 0.01 && Math.abs(globalLon - (-77.036)) < 0.01) ||
                    (Math.abs(globalLat - 37.7749) < 0.01 && Math.abs(globalLon - (-122.4194)) < 0.01) ||
                    (Math.abs(globalLat - 40.7128) < 0.01 && Math.abs(globalLon - (-74.0060)) < 0.01)) {
                    isMock = true;
                    // Clear from Global storage
                    Global.setLocation(null);
                    Log.w("Location", "Cleared mock location from Global storage: " + globalLat + ", " + globalLon);
                }
            }
            
            // Clear from SharedPreferences if mock detected
            if (isMock && (lat != 0.0f || lon != 0.0f)) {
                android.content.SharedPreferences.Editor editor = prefs.edit();
                editor.remove("latitude");
                editor.remove("longitude");
                editor.remove("altitude");
                editor.remove("location_timestamp");
                editor.remove("location_accuracy");
                editor.apply();
                Log.w("Location", "Cleared mock location from SharedPreferences: " + lat + ", " + lon);
            }
        } catch (Exception e) {
            Log.e("Location", "Error clearing mock location from storage", e);
        }
    }
}
