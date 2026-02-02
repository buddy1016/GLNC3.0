package com.example.glnc.ui.map;

import android.Manifest;
import android.content.Context;
import android.content.pm.PackageManager;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

import androidx.annotation.NonNull;
import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;
import androidx.fragment.app.Fragment;

import com.example.glnc.Location;
import com.example.glnc.MainActivity;
import com.example.glnc.R;
import com.example.glnc.databinding.FragmentMapBinding;

import org.osmdroid.api.IMapController;
import org.osmdroid.config.Configuration;
import org.osmdroid.tileprovider.tilesource.TileSourceFactory;
import org.osmdroid.util.GeoPoint;
import org.osmdroid.views.MapView;
import org.osmdroid.views.overlay.Marker;
import org.osmdroid.views.overlay.mylocation.GpsMyLocationProvider;
import org.osmdroid.views.overlay.mylocation.MyLocationNewOverlay;

public class MapFragment extends Fragment {

    private FragmentMapBinding binding;
    private MapView mapView;
    private IMapController mapController;
    private MyLocationNewOverlay myLocationOverlay;
    private Marker currentLocationMarker;
    private boolean isMapReady = false;
    private Handler locationUpdateHandler;
    private Runnable locationUpdateRunnable;
    private static final long LOCATION_UPDATE_INTERVAL = 2000; // Update map every 2 seconds

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        
        // Configure OSMDroid
        Context ctx = requireContext().getApplicationContext();
        Configuration.getInstance().load(ctx, ctx.getSharedPreferences("osmdroid", Context.MODE_PRIVATE));
        Configuration.getInstance().setUserAgentValue(ctx.getPackageName());
    }

    @Override
    public View onCreateView(@NonNull LayoutInflater inflater,
                             ViewGroup container, Bundle savedInstanceState) {
        binding = FragmentMapBinding.inflate(inflater, container, false);
        View root = binding.getRoot();

        // Initialize map
        mapView = binding.mapView;
        mapView.setTileSource(TileSourceFactory.MAPNIK);
        mapView.setMultiTouchControls(true);
        
        // Get map controller
        mapController = mapView.getController();
        mapController.setZoom(15.0); // Set initial zoom level
        
        // Check location permission
        if (ActivityCompat.checkSelfPermission(requireContext(), Manifest.permission.ACCESS_FINE_LOCATION)
                == PackageManager.PERMISSION_GRANTED ||
            ActivityCompat.checkSelfPermission(requireContext(), Manifest.permission.ACCESS_COARSE_LOCATION)
                == PackageManager.PERMISSION_GRANTED) {
            setupLocationOverlay();
            startLocationUpdates();
        } else {
            Log.w("MapFragment", "Location permission not granted");
        }

        return root;
    }

    @Override
    public void onResume() {
        super.onResume();
        if (mapView != null) {
            mapView.onResume();
        }
        
        // Start location updates when fragment resumes
        startLocationUpdates();
    }

    @Override
    public void onPause() {
        super.onPause();
        if (mapView != null) {
            mapView.onPause();
        }
        
        // Stop location updates when fragment pauses
        stopLocationUpdates();
    }

    @Override
    public void onDestroyView() {
        super.onDestroyView();
        // Stop location updates
        stopLocationUpdates();
        
        if (mapView != null) {
            mapView.onDetach();
        }
        binding = null;
    }

    private void setupLocationOverlay() {
        // Create location overlay for showing current location
        myLocationOverlay = new MyLocationNewOverlay(new GpsMyLocationProvider(requireContext()), mapView);
        myLocationOverlay.enableMyLocation();
        mapView.getOverlays().add(myLocationOverlay);
    }

    /**
     * Start continuous location updates from MainActivity's Location instance
     * Updates map every 2 seconds with current GPS coordinates
     */
    private void startLocationUpdates() {
        // Stop any existing updates
        stopLocationUpdates();
        
        // Create handler on main thread
        locationUpdateHandler = new Handler(Looper.getMainLooper());
        
        // Create runnable to update map with current location
        locationUpdateRunnable = new Runnable() {
            @Override
            public void run() {
                // Get Location instance from MainActivity
                MainActivity mainActivity = (MainActivity) getActivity();
                if (mainActivity != null && mainActivity.location != null) {
                    Location location = mainActivity.location;
                    
                    // Check if location is available (not 0,0)
                    if (location.latitude != 0.0 && location.longitude != 0.0) {
                        updateMapWithLocation(location.latitude, location.longitude);
                    } else {
                        // Try to use stored location as fallback
                        android.content.SharedPreferences prefs = 
                            requireContext().getSharedPreferences("GLNC_Prefs", Context.MODE_PRIVATE);
                        float latitude = prefs.getFloat("latitude", 0.0f);
                        float longitude = prefs.getFloat("longitude", 0.0f);
                        
                        if (latitude != 0.0f || longitude != 0.0f) {
                            Log.d("MapFragment", "Using stored location: " + latitude + ", " + longitude);
                            updateMapWithLocation(latitude, longitude);
                        } else {
                            Log.d("MapFragment", "Waiting for GPS location...   " + latitude);
                        }
                    }
                }
                
                // Schedule next update
                if (locationUpdateHandler != null) {
                    locationUpdateHandler.postDelayed(this, LOCATION_UPDATE_INTERVAL);
                }
            }
        };
        
        // Start updates immediately, then schedule periodic updates
        locationUpdateHandler.post(locationUpdateRunnable);
        Log.d("MapFragment", "Started continuous location updates for map");
    }

    /**
     * Stop location updates
     */
    private void stopLocationUpdates() {
        if (locationUpdateHandler != null && locationUpdateRunnable != null) {
            locationUpdateHandler.removeCallbacks(locationUpdateRunnable);
            locationUpdateHandler = null;
            locationUpdateRunnable = null;
            Log.d("MapFragment", "Stopped location updates");
        }
    }

    /**
     * Update map with given coordinates
     */
    private void updateMapWithLocation(double latitude, double longitude) {
        if (mapView == null || mapController == null) {
            return;
        }
        
        Log.d("MapFragment", "Updating map with location: " + latitude + ", " + longitude);
        
        // Create GeoPoint for the location
        GeoPoint currentLocation = new GeoPoint(latitude, longitude);
        
        // Remove existing marker if any
        if (currentLocationMarker != null) {
            mapView.getOverlays().remove(currentLocationMarker);
        }
        
        // Create and add marker for current location
        currentLocationMarker = new Marker(mapView);
        currentLocationMarker.setPosition(currentLocation);
        currentLocationMarker.setAnchor(Marker.ANCHOR_CENTER, Marker.ANCHOR_BOTTOM);
        currentLocationMarker.setTitle("Your Location");
        currentLocationMarker.setSnippet(String.format("Lat: %.6f, Lon: %.6f", latitude, longitude));
        
        // Set a custom icon
        currentLocationMarker.setIcon(ContextCompat.getDrawable(requireContext(), android.R.drawable.ic_menu_mylocation));
        
        mapView.getOverlays().add(currentLocationMarker);
        
        // Center map on current location (only on first update)
        if (!isMapReady) {
            mapController.setCenter(currentLocation);
            isMapReady = true;
        }
        
        // Enable my location overlay to show blue dot
        if (myLocationOverlay != null) {
            myLocationOverlay.enableMyLocation();
        }
        
        mapView.invalidate(); // Refresh map
    }
}

