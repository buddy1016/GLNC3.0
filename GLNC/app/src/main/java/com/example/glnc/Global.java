package com.example.glnc;

import android.content.Context;
import android.content.SharedPreferences;
import android.location.Location;
import android.location.LocationManager;
import android.util.Log;

/**
 * Global application configuration and location storage
 * Provides unified location storage matching Worktime-Famoco documentation
 * Uses LocationManager-based location system (no Google Play Services)
 */
public class Global {
//    public String serverUrl = "http://192.168.145.90:32646/api";
    public String serverUrl = "https://2ts.myrfid.nc/api";
    
    // Unified location storage (matches documentation: Globals.setLocation/getLocation)
    private static Location currentLocation = null;
    
    /**
     * Store location in Global class for unified access
     * Matches documentation: Globals.setLocation(location)
     * @param location The location to store (null to clear)
     */
    public static void setLocation(Location location) {
        if (location != null) {
            currentLocation = location;
            Log.d("Global", "Location stored in Global: " + location.getLatitude() + ", " + location.getLongitude());
        } else {
            currentLocation = null;
            Log.d("Global", "Location cleared from Global storage");
        }
    }
    
    /**
     * Get stored location from Global class
     * Matches documentation: Globals.getLocation()
     * @return The stored location, or null if not available
     */
    public static Location getLocation() {
        return currentLocation;
    }
    
    /**
     * Interface for location callback (for backward compatibility)
     */
    public interface LocationCallback {
        void onLocationReceived(double latitude, double longitude, double altitude);
        void onLocationError(String error);
    }
    
    /**
     * Get current location using stored location from Global or Location instance
     * This method provides location from the unified LocationManager-based system
     * @param context The context to use
     * @param callback The callback to receive location data or error
     */
    public static void getCurrentLocation(Context context, LocationCallback callback) {
        getCurrentLocation(context, callback, false);
    }
    
    /**
     * Get current location using stored location from Global or Location instance
     * @param context The context to use
     * @param callback The callback to receive location data or error
     * @param forceFresh If true, requires location to be very recent (for critical operations)
     */
    public static void getCurrentLocation(Context context, LocationCallback callback, boolean forceFresh) {
        if (context == null || callback == null) {
            if (callback != null) {
                callback.onLocationError("Invalid context or callback");
            }
            return;
        }
        
        // First try to get location from Global storage
        Location location = getLocation();
        
        if (location != null) {
            long age = System.currentTimeMillis() - location.getTime();
            long maxAge = forceFresh ? 10000 : 300000; // 10 seconds for fresh, 5 minutes otherwise
            
            if (age <= maxAge) {
                Log.d("Global", "Using stored location from Global (age: " + (age / 1000) + "s)");
                callback.onLocationReceived(
                    location.getLatitude(),
                    location.getLongitude(),
                    location.getAltitude()
                );
                return;
            } else {
                Log.w("Global", "Stored location too old: " + (age / 1000) + "s, max: " + (maxAge / 1000) + "s");
            }
        }
        
        // Fallback: Try to get location from SharedPreferences
        SharedPreferences prefs = context.getSharedPreferences("GLNC_Prefs", Context.MODE_PRIVATE);
        float latitude = prefs.getFloat("latitude", 0.0f);
        float longitude = prefs.getFloat("longitude", 0.0f);
        float altitude = prefs.getFloat("altitude", 0.0f);
        long timestamp = prefs.getLong("location_timestamp", 0);
        
        if (latitude != 0.0f || longitude != 0.0f) {
            long age = timestamp > 0 ? System.currentTimeMillis() - timestamp : Long.MAX_VALUE;
            long maxAge = forceFresh ? 10000 : 300000;
            
            if (age <= maxAge) {
                Log.d("Global", "Using location from SharedPreferences (age: " + (age / 1000) + "s)");
                callback.onLocationReceived(latitude, longitude, altitude);
                return;
            } else {
                Log.w("Global", "SharedPreferences location too old: " + (age / 1000) + "s");
            }
        }
        
        // If no valid location found, return error
        Log.e("Global", "No valid location available");
        callback.onLocationError("No location available. Please wait for GPS fix.");
    }
    
    /**
     * Check if location services are enabled
     * @param context The context to use
     * @return true if location services are enabled
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
                    locationEnabled = gpsEnabled || networkEnabled;
                }
            } catch (Exception e) {
                locationEnabled = gpsEnabled || networkEnabled;
            }
            
            Log.d("Global", "Location Services Check - GPS Enabled: " + gpsEnabled + 
                  ", Network Enabled: " + networkEnabled + 
                  ", Location Enabled: " + locationEnabled);
            
            return locationEnabled;
        } catch (Exception e) {
            Log.e("Global", "Error checking location services", e);
            return true; // Return true to continue - might still work
        }
    }
}
