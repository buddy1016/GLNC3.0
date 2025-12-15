package com.example.glnc;

import android.Manifest;
import android.content.Context;
import android.content.pm.PackageManager;
import android.location.Location;
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
    public String serverUrl = "http://192.168.145.90:32646/api";
//    public  String serverUrl = "https://2ts.myrfid.nc/api";
    /**
     * Interface for location callback
     */
    public interface LocationCallback {
        void onLocationReceived(double latitude, double longitude, double altitude);
        void onLocationError(String error);
    }

    /**
     * Get current location using GPS
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

        // Check permission
        if (ActivityCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION)
                != PackageManager.PERMISSION_GRANTED &&
            ActivityCompat.checkSelfPermission(context, Manifest.permission.ACCESS_COARSE_LOCATION)
                != PackageManager.PERMISSION_GRANTED) {
            Log.e("Global", "Location permission not granted");
            callback.onLocationError("Location permission not granted");
            return;
        }


        // Get location using FusedLocationProviderClient
        FusedLocationProviderClient fusedLocationClient = LocationServices.getFusedLocationProviderClient(context);
        
        // First try to get last known location (fast)
        fusedLocationClient.getLastLocation()
                .addOnSuccessListener(new OnSuccessListener<Location>() {
                    @Override
                    public void onSuccess(Location location) {
                        if (location != null) {
                            // Use last known location if available and recent (within 1 minute)
                            long timeDelta = System.currentTimeMillis() - location.getTime();
                            if (timeDelta < 60000) { // 1 minute
                                double latitude = location.getLatitude();
                                double longitude = location.getLongitude();
                                double altitude = location.getAltitude();
                                
                              Log.d("Global", "=============================");
                                callback.onLocationReceived(latitude, longitude, altitude);
                                return;
                            }
                        }
                        
                        // If no recent cached location, request fresh location
                        requestFreshLocation(context, fusedLocationClient, callback);
                    }
                })
                .addOnFailureListener(e -> {
                    Log.w("Global", "Failed to get last location, requesting fresh location", e);
                    // If getLastLocation fails, request fresh location
                    requestFreshLocation(context, fusedLocationClient, callback);
                });
    }
    
    private static void requestFreshLocation(Context context, FusedLocationProviderClient fusedLocationClient, LocationCallback callback) {
        // Create location request for high accuracy GPS
        LocationRequest locationRequest = LocationRequest.create()
                .setPriority(Priority.PRIORITY_HIGH_ACCURACY)
                .setInterval(1000)
                .setFastestInterval(500)
                .setMaxWaitTime(15000) // Wait up to 15 seconds
                .setNumUpdates(1); // Only need one update
        
        // Handler for timeout
        Handler handler = new Handler(Looper.getMainLooper());
        final boolean[] locationReceived = {false};
        
        // Create location callback
        com.google.android.gms.location.LocationCallback locationCallback = new com.google.android.gms.location.LocationCallback() {
            @Override
            public void onLocationResult(LocationResult locationResult) {
                if (locationResult != null && locationResult.getLastLocation() != null && !locationReceived[0]) {
                    locationReceived[0] = true;
                    fusedLocationClient.removeLocationUpdates(this);
                    handler.removeCallbacksAndMessages(null);
                    
                    Location location = locationResult.getLastLocation();
                    double latitude = location.getLatitude();
                    double longitude = location.getLongitude();
                    double altitude = location.getAltitude();
                    
                  Log.d("Global", "===================================");
                    callback.onLocationReceived(latitude, longitude, altitude);
                }
            }
        };
        
        // Set timeout - if no location received in 15 seconds, try getCurrentLocation as fallback
        handler.postDelayed(() -> {
            if (!locationReceived[0]) {
                fusedLocationClient.removeLocationUpdates(locationCallback);
                Log.w("Global", "Location update timeout, trying getCurrentLocation");
                
                // Fallback to getCurrentLocation
                try {
                    fusedLocationClient.getCurrentLocation(Priority.PRIORITY_HIGH_ACCURACY, null)
                        .addOnSuccessListener(new OnSuccessListener<Location>() {
                            @Override
                            public void onSuccess(Location location) {
                                if (location != null && !locationReceived[0]) {
                                    locationReceived[0] = true;
                                    double latitude = location.getLatitude();
                                    double longitude = location.getLongitude();
                                    double altitude = location.getAltitude();
                                    
                                    Log.d("Global", "=================================");
                                    callback.onLocationReceived(latitude, longitude, altitude);
                                } else if (!locationReceived[0]) {
                                    Log.w("Global", "getCurrentLocation also returned null");
                                    callback.onLocationError("Location not available after timeout");
                                }
                            }
                        })
                        .addOnFailureListener(e -> {
                            if (!locationReceived[0]) {
                                Log.e("Global", "Error in getCurrentLocation fallback", e);
                                callback.onLocationError("Failed to get location: " + e.getMessage());
                            }
                        });
                } catch (Exception e) {
                    if (!locationReceived[0]) {
                        Log.e("Global", "Exception in getCurrentLocation fallback", e);
                        callback.onLocationError("Location request failed: " + e.getMessage());
                    }
                }
            }
        }, 15000); // 15 second timeout
        
        // Request location updates
        try {
            if (ActivityCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION)
                    == PackageManager.PERMISSION_GRANTED ||
                ActivityCompat.checkSelfPermission(context, Manifest.permission.ACCESS_COARSE_LOCATION)
                    == PackageManager.PERMISSION_GRANTED) {
                fusedLocationClient.requestLocationUpdates(locationRequest, locationCallback, Looper.getMainLooper())
                    .addOnFailureListener(e -> {
                        if (!locationReceived[0]) {
                            Log.e("Global", "Error requesting location updates", e);
                            handler.removeCallbacksAndMessages(null);
                            callback.onLocationError("Failed to request location updates: " + e.getMessage());
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
}
