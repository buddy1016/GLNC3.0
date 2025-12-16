package com.example.glnc;

import android.Manifest;
import android.content.Context;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.location.Location;
import android.location.LocationManager;
import android.provider.Settings;
import android.util.Log;

import androidx.core.app.ActivityCompat;

import android.os.Handler;
import android.os.Looper;

import com.google.android.gms.location.FusedLocationProviderClient;
import com.google.android.gms.location.LocationRequest;
import com.google.android.gms.location.LocationResult;
import com.google.android.gms.location.LocationServices;
import com.google.android.gms.location.Priority;
import com.google.android.gms.tasks.OnSuccessListener;

public class Global {
//    public String serverUrl = "http://192.168.145.90:32646/api";
    public  String serverUrl = "https://2ts.myrfid.nc/api";
    
    // Constants for GPS validation and TTFF (Time To First Fix)
    private static final long TTFF_TIMEOUT_MS = 60000; // 60 seconds for TTFF per Famoco requirements
    private static final float MIN_ACCURACY_METERS = 50.0f; // Minimum acceptable accuracy (preferred)
    private static final long MAX_LOCATION_AGE_MS = 120000; // 2 minutes max age for cached location
    private static final long RECENT_LOCATION_THRESHOLD_MS = 30000; // 30 seconds for recent cached location
    
    /**
     * Interface for location callback
     */
    public interface LocationCallback {
        void onLocationReceived(double latitude, double longitude, double altitude);
        void onLocationError(String error);
    }

    /**
     * Check if location services are enabled and in high accuracy mode
     * Per Famoco MDM requirements: High accuracy mode must be enabled
     * Uses reliable method that works across all Android versions
     */
    public static boolean isLocationEnabled(Context context) {
        try {
            LocationManager locationManager = (LocationManager) context.getSystemService(Context.LOCATION_SERVICE);
            if (locationManager == null) {
                return false;
            }
            
            boolean gpsEnabled = locationManager.isProviderEnabled(LocationManager.GPS_PROVIDER);
            boolean networkEnabled = locationManager.isProviderEnabled(LocationManager.NETWORK_PROVIDER);
            
            // For Android API 28+, use isLocationEnabled() if available
            boolean locationEnabled = false;
            try {
                if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.P) {
                    locationEnabled = locationManager.isLocationEnabled();
                } else {
                    // For older versions, check if at least one provider is enabled
                    locationEnabled = gpsEnabled || networkEnabled;
                }
            } catch (Exception e) {
                // Fallback: if isLocationEnabled() not available, check providers
                locationEnabled = gpsEnabled || networkEnabled;
            }
            
            // High accuracy mode determination:
            // - GPS enabled is the primary requirement (GPS provides high accuracy)
            // - Network enabled is a bonus but not strictly required for high accuracy
            // - On emulators/devices, network might be disabled but GPS still works
            // - If GPS is enabled, we consider it high accuracy capable
            // - Both GPS and Network enabled = ideal high accuracy mode
            boolean highAccuracyMode = gpsEnabled; // GPS is the key requirement for high accuracy
            
            Log.d("Global", "Location Services Check - GPS Enabled: " + gpsEnabled + 
                  ", Network Enabled: " + networkEnabled + 
                  ", Location Enabled: " + locationEnabled +
                  ", High Accuracy Mode: " + highAccuracyMode + 
                  (gpsEnabled && networkEnabled ? " (Ideal: GPS + Network)" : 
                   gpsEnabled ? " (GPS only - sufficient for high accuracy)" : " (Not enabled)"));
            
            return gpsEnabled && highAccuracyMode;
        } catch (Exception e) {
            Log.e("Global", "Error checking location services", e);
            // Return true to continue - might still work
            return true;
        }
    }

    /**
     * Validate if location is from GPS and has acceptable accuracy
     * Ensures we get actual GPS fix, not just network location
     */
    private static boolean isValidGpsLocation(Location location) {
        if (location == null) {
            return false;
        }
        
        // Check if location is recent
        long age = System.currentTimeMillis() - location.getTime();
        if (age > MAX_LOCATION_AGE_MS) {
            Log.w("Global", "Location too old: " + (age / 1000) + " seconds");
            return false;
        }
        
        // Check accuracy - log warning if accuracy is low but still accept
        if (location.hasAccuracy()) {
            if (location.getAccuracy() > MIN_ACCURACY_METERS) {
                Log.w("Global", "Location accuracy lower than preferred: " + location.getAccuracy() + "m (preferred: " + MIN_ACCURACY_METERS + "m)");
            } else {
                Log.d("Global", "Location accuracy acceptable: " + location.getAccuracy() + "m");
            }
        } else {
            Log.w("Global", "Location accuracy unknown");
        }
        
        // Check provider - prefer GPS provider
        String provider = location.getProvider();
        boolean isGpsProvider = LocationManager.GPS_PROVIDER.equals(provider) || 
                                 "gps".equalsIgnoreCase(provider) ||
                                 "fused".equalsIgnoreCase(provider);
        
        Log.d("Global", "Location Validation - Provider: " + provider + 
              ", Accuracy: " + (location.hasAccuracy() ? location.getAccuracy() + "m" : "unknown") +
              ", Age: " + (age / 1000) + "s, IsGPS: " + isGpsProvider);
        
        return true; // Accept location if it passes basic checks
    }

    /**
     * Get current location using GPS with enhanced validation
     * This method ensures GPS fix is obtained (TTFF) as per Famoco MDM requirements
     * @param context The context to use for location services
     * @param callback The callback to receive location data or error
     */
    public static void getCurrentLocation(Context context, LocationCallback callback) {

        if (context == null || callback == null) {
            if (callback != null) {
                callback.onLocationError("Invalid context or callback");
            }
            return;
        }

        // Check permission - require FINE_LOCATION for GPS
        if (ActivityCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION)
                != PackageManager.PERMISSION_GRANTED) {
            Log.e("Global", "ACCESS_FINE_LOCATION permission not granted");
            callback.onLocationError("Location permission not granted");
            return;
        }

        // Check if location services are enabled (log warning but continue)
        if (!isLocationEnabled(context)) {
            Log.w("Global", "Location services not enabled or not in high accuracy mode. Continuing anyway...");
        }

        FusedLocationProviderClient fusedLocationClient = LocationServices.getFusedLocationProviderClient(context);
        
        // First try to get last known location (fast path)
        fusedLocationClient.getLastLocation()
                .addOnSuccessListener(new OnSuccessListener<Location>() {
                    @Override
                    public void onSuccess(Location location) {
                        if (location != null && isValidGpsLocation(location)) {
                            long age = System.currentTimeMillis() - location.getTime();
                            // Use cached location if very recent (within 30 seconds) and valid
                            if (age < RECENT_LOCATION_THRESHOLD_MS) {
                                Log.d("Global", "Using recent cached location (age: " + (age / 1000) + "s)");
                                callback.onLocationReceived(
                                    location.getLatitude(),
                                    location.getLongitude(),
                                    location.getAltitude()
                                );
                                // Store location for future use
                                storeLocation(context, location);
                                return;
                            }
                        }
                        
                        // Request fresh GPS location with extended timeout for TTFF
                        requestFreshGpsLocation(context, fusedLocationClient, callback);
                    }
                })
                .addOnFailureListener(e -> {
                    Log.w("Global", "Failed to get last location, requesting fresh GPS location", e);
                    requestFreshGpsLocation(context, fusedLocationClient, callback);
                });
    }
    
    /**
     * Request fresh GPS location with extended timeout for TTFF (Time To First Fix)
     * Per Famoco requirements: Allow up to 60 seconds for GPS fix
     */
    private static void requestFreshGpsLocation(Context context, 
                                                FusedLocationProviderClient fusedLocationClient, 
                                                LocationCallback callback) {
        Log.d("Global", "Requesting fresh GPS location with TTFF timeout: " + (TTFF_TIMEOUT_MS / 1000) + " seconds");
        
        // Create location request for high accuracy GPS
        LocationRequest locationRequest = LocationRequest.create()
                .setPriority(Priority.PRIORITY_HIGH_ACCURACY) // Force GPS usage
                .setInterval(1000)
                .setFastestInterval(500)
                .setMaxWaitTime(TTFF_TIMEOUT_MS) // Extended timeout for TTFF
                .setNumUpdates(1)
                .setWaitForAccurateLocation(true); // Wait for accurate location
        
        Handler handler = new Handler(Looper.getMainLooper());
        final boolean[] locationReceived = {false};
        final Location[] bestLocation = {null};
        
        // Create location callback
        com.google.android.gms.location.LocationCallback locationCallback = 
            new com.google.android.gms.location.LocationCallback() {
            @Override
            public void onLocationResult(LocationResult locationResult) {
                if (locationResult != null && locationResult.getLastLocation() != null) {
                    Location location = locationResult.getLastLocation();
                    
                    // Validate location
                    if (isValidGpsLocation(location)) {
                        // Keep track of best location (most accurate)
                        if (bestLocation[0] == null || 
                            (location.hasAccuracy() && bestLocation[0].hasAccuracy() &&
                             location.getAccuracy() < bestLocation[0].getAccuracy())) {
                            bestLocation[0] = location;
                        }
                        
                        // Accept location if it's accurate enough
                        if (location.hasAccuracy() && location.getAccuracy() <= MIN_ACCURACY_METERS) {
                            if (!locationReceived[0]) {
                                locationReceived[0] = true;
                                fusedLocationClient.removeLocationUpdates(this);
                                handler.removeCallbacksAndMessages(null);
                                
                                Log.d("Global", "GPS fix obtained! Accuracy: " + location.getAccuracy() + "m");
                                callback.onLocationReceived(
                                    location.getLatitude(),
                                    location.getLongitude(),
                                    location.getAltitude()
                                );
                                storeLocation(context, location);
                            }
                            return;
                        }
                    }
                }
            }
        };
        
        // Extended timeout for TTFF - per Famoco requirements (60 seconds)
        handler.postDelayed(() -> {
            if (!locationReceived[0]) {
                fusedLocationClient.removeLocationUpdates(locationCallback);
                
                // Use best location found, even if not perfect
                if (bestLocation[0] != null && isValidGpsLocation(bestLocation[0])) {
                    Log.w("Global", "Using best available location after TTFF timeout. Accuracy: " + 
                          (bestLocation[0].hasAccuracy() ? bestLocation[0].getAccuracy() + "m" : "unknown"));
                    locationReceived[0] = true;
                    callback.onLocationReceived(
                        bestLocation[0].getLatitude(),
                        bestLocation[0].getLongitude(),
                        bestLocation[0].getAltitude()
                    );
                    storeLocation(context, bestLocation[0]);
                } else {
                    // Final fallback: try getCurrentLocation
                    Log.w("Global", "TTFF timeout reached, trying getCurrentLocation as final fallback");
                    try {
                        fusedLocationClient.getCurrentLocation(Priority.PRIORITY_HIGH_ACCURACY, null)
                            .addOnSuccessListener(new OnSuccessListener<Location>() {
                                @Override
                                public void onSuccess(Location location) {
                                    if (location != null && !locationReceived[0]) {
                                        locationReceived[0] = true;
                                        if (isValidGpsLocation(location)) {
                                            Log.d("Global", "Fallback location obtained");
                                            callback.onLocationReceived(
                                                location.getLatitude(),
                                                location.getLongitude(),
                                                location.getAltitude()
                                            );
                                            storeLocation(context, location);
                                        } else {
                                            callback.onLocationError("Location obtained but failed validation");
                                        }
                                    } else if (!locationReceived[0]) {
                                        callback.onLocationError("GPS fix not obtained within " + 
                                                                 (TTFF_TIMEOUT_MS / 1000) + " seconds");
                                    }
                                }
                            })
                            .addOnFailureListener(e -> {
                                if (!locationReceived[0]) {
                                    Log.e("Global", "Final fallback also failed", e);
                                    callback.onLocationError("Failed to obtain GPS fix: " + e.getMessage());
                                }
                            });
                    } catch (Exception e) {
                        if (!locationReceived[0]) {
                            Log.e("Global", "Exception in final fallback", e);
                            callback.onLocationError("GPS location request failed: " + e.getMessage());
                        }
                    }
                }
            }
        }, TTFF_TIMEOUT_MS);
        
        // Request location updates
        try {
            if (ActivityCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION)
                    == PackageManager.PERMISSION_GRANTED) {
                fusedLocationClient.requestLocationUpdates(locationRequest, locationCallback, Looper.getMainLooper())
                    .addOnFailureListener(e -> {
                        if (!locationReceived[0]) {
                            Log.e("Global", "Error requesting location updates", e);
                            handler.removeCallbacksAndMessages(null);
                            callback.onLocationError("Failed to request GPS location: " + e.getMessage());
                        }
                    });
            } else {
                handler.removeCallbacksAndMessages(null);
                callback.onLocationError("Location permission not granted");
            }
        } catch (Exception e) {
            handler.removeCallbacksAndMessages(null);
            Log.e("Global", "Exception requesting location updates", e);
            callback.onLocationError("Location request failed: " + e.getMessage());
        }
    }
    
    /**
     * Store location in SharedPreferences for fallback use
     */
    private static void storeLocation(Context context, Location location) {
        try {
            SharedPreferences prefs = context.getSharedPreferences("GLNC_Prefs", Context.MODE_PRIVATE);
            SharedPreferences.Editor editor = prefs.edit();
            editor.putFloat("latitude", (float) location.getLatitude());
            editor.putFloat("longitude", (float) location.getLongitude());
            editor.putFloat("altitude", (float) location.getAltitude());
            editor.putLong("location_timestamp", location.getTime());
            if (location.hasAccuracy()) {
                editor.putFloat("location_accuracy", location.getAccuracy());
            }
            editor.apply();
            Log.d("Global", "Location stored: " + location.getLatitude() + ", " + location.getLongitude() + 
                  (location.hasAccuracy() ? " (accuracy: " + location.getAccuracy() + "m)" : ""));
        } catch (Exception e) {
            Log.e("Global", "Error storing location", e);
        }
    }
}
