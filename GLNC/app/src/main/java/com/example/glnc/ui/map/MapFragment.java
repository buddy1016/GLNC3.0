package com.example.glnc.ui.map;

import android.Manifest;
import android.content.Context;
import android.content.pm.PackageManager;
import android.os.Bundle;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

import androidx.annotation.NonNull;
import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;
import androidx.fragment.app.Fragment;

import com.example.glnc.Global;
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
            getCurrentLocationAndUpdateMap();
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
        
        // Update location when fragment resumes
        if (isMapReady) {
            getCurrentLocationAndUpdateMap();
        }
    }

    @Override
    public void onPause() {
        super.onPause();
        if (mapView != null) {
            mapView.onPause();
        }
    }

    @Override
    public void onDestroyView() {
        super.onDestroyView();
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

    private void getCurrentLocationAndUpdateMap() {
        Log.d("MapFragment", "Requesting current location for map...");
        
        Global.getCurrentLocation(requireContext(), new Global.LocationCallback() {
            @Override
            public void onLocationReceived(double latitude, double longitude, double altitude) {
                Log.d("MapFragment", "Location received: " + latitude + ", " + longitude);
                
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
                currentLocationMarker.setSnippet("Lat: " + latitude + ", Lon: " + longitude);
                
                // Set a custom icon (using default for now, can be customized)
                currentLocationMarker.setIcon(ContextCompat.getDrawable(requireContext(), android.R.drawable.ic_menu_mylocation));
                
                mapView.getOverlays().add(currentLocationMarker);
                
                // Center map on current location
                mapController.setCenter(currentLocation);
                
                // Enable my location overlay to show blue dot
                if (myLocationOverlay != null) {
                    myLocationOverlay.enableMyLocation();
                }
                
                isMapReady = true;
                mapView.invalidate(); // Refresh map
                
                Log.d("MapFragment", "Map updated with current location");
            }

            @Override
            public void onLocationError(String error) {
                Log.e("MapFragment", "Failed to get location: " + error);
                
                // Try to use stored location as fallback
                android.content.SharedPreferences prefs = requireContext().getSharedPreferences("GLNC_Prefs", Context.MODE_PRIVATE);
                float latitude = prefs.getFloat("latitude", 0.0f);
                float longitude = prefs.getFloat("longitude", 0.0f);
                
                if (latitude != 0.0f || longitude != 0.0f) {
                    Log.d("MapFragment", "Using stored location as fallback");
                    GeoPoint fallbackLocation = new GeoPoint(latitude, longitude);
                    mapController.setCenter(fallbackLocation);
                    
                    // Add marker for stored location
                    if (currentLocationMarker != null) {
                        mapView.getOverlays().remove(currentLocationMarker);
                    }
                    currentLocationMarker = new Marker(mapView);
                    currentLocationMarker.setPosition(fallbackLocation);
                    currentLocationMarker.setAnchor(Marker.ANCHOR_CENTER, Marker.ANCHOR_BOTTOM);
                    currentLocationMarker.setTitle("Your Location (Stored)");
                    currentLocationMarker.setSnippet("Lat: " + latitude + ", Lon: " + longitude);
                    currentLocationMarker.setIcon(ContextCompat.getDrawable(requireContext(), android.R.drawable.ic_menu_mylocation));
                    mapView.getOverlays().add(currentLocationMarker);
                    mapView.invalidate();
                } else {
                    // Set default location (can be changed to a specific default)
                    GeoPoint defaultLocation = new GeoPoint(38.9072, -77.0369); // Washington DC as default
                    mapController.setCenter(defaultLocation);
                    Log.w("MapFragment", "No location available, using default location");
                }
                
                isMapReady = true;
            }
        });
    }
}

