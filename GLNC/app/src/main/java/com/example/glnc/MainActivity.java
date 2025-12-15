package com.example.glnc;

import android.Manifest;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.location.Location;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.util.Log;
import org.osmdroid.config.Configuration;
import android.view.View;
import android.view.Menu;

import com.google.android.gms.location.FusedLocationProviderClient;
import com.google.android.gms.location.LocationServices;
import com.google.android.gms.tasks.OnSuccessListener;
import com.google.android.material.snackbar.Snackbar;
import com.google.android.material.navigation.NavigationView;

import androidx.annotation.NonNull;
import androidx.core.app.ActivityCompat;
import androidx.navigation.NavController;
import androidx.navigation.Navigation;
import androidx.navigation.ui.AppBarConfiguration;
import androidx.navigation.ui.NavigationUI;
import androidx.drawerlayout.widget.DrawerLayout;
import androidx.appcompat.app.AppCompatActivity;

import com.example.glnc.databinding.ActivityMainBinding;

import org.json.JSONObject;

import java.io.IOException;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.concurrent.TimeUnit;

import okhttp3.Call;
import okhttp3.Callback;
import okhttp3.MediaType;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.RequestBody;
import okhttp3.Response;

public class MainActivity extends AppCompatActivity {

    private AppBarConfiguration mAppBarConfiguration;
    private ActivityMainBinding binding;
    private OkHttpClient httpClient;
    private Global global = new Global();
    private FusedLocationProviderClient fusedLocationClient;
    private boolean isLoggingOut = false;
    private NavController navController;
    private Handler locationUpdateHandler;
    private Runnable locationUpdateRunnable;
    private static final long LOCATION_UPDATE_INTERVAL = 5 * 60 * 1000; // 5 minutes in milliseconds

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        binding = ActivityMainBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());

        // Configure OSMDroid
        Configuration.getInstance().load(this, getSharedPreferences("osmdroid", MODE_PRIVATE));
        Configuration.getInstance().setUserAgentValue(getPackageName());
        
        // Initialize OkHttpClient
        httpClient = new OkHttpClient.Builder()
                .connectTimeout(10, TimeUnit.SECONDS)
                .readTimeout(10, TimeUnit.SECONDS)
                .writeTimeout(10, TimeUnit.SECONDS)
                .build();

        // Initialize location client
        fusedLocationClient = LocationServices.getFusedLocationProviderClient(this);

        setSupportActionBar(binding.appBarMain.toolbar);
        binding.appBarMain.fab.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                // Refresh delivery list
                refreshHomeFragment();
            }
        });
        DrawerLayout drawer = binding.drawerLayout;
        NavigationView navigationView = binding.navView;
        // Passing each menu ID as a set of Ids because each
        // menu should be considered as top level destinations.
        mAppBarConfiguration = new AppBarConfiguration.Builder(
                R.id.nav_home, R.id.nav_map)
                .setOpenableLayout(drawer)
                .build();
        NavController navController = Navigation.findNavController(this, R.id.nav_host_fragment_content_main);
        NavigationUI.setupActionBarWithNavController(this, navController, mAppBarConfiguration);
        
        // Store navController for refresh functionality
        this.navController = navController;
        
        // Listen to navigation changes to show/hide FAB
        navController.addOnDestinationChangedListener(new androidx.navigation.NavController.OnDestinationChangedListener() {
            @Override
            public void onDestinationChanged(@NonNull androidx.navigation.NavController controller,
                                           @NonNull androidx.navigation.NavDestination destination,
                                           Bundle arguments) {
                // Hide FAB when on Map fragment, show it on Home fragment
                if (destination.getId() == R.id.nav_map) {
                    binding.appBarMain.fab.setVisibility(View.GONE);
                } else if (destination.getId() == R.id.nav_home) {
                    binding.appBarMain.fab.setVisibility(View.VISIBLE);
                }
            }
        });
        
        // Update navigation header with user name
        View headerView = navigationView.getHeaderView(0);
        android.widget.TextView userNameTextView = headerView.findViewById(R.id.textView);
        SharedPreferences prefs = getSharedPreferences("GLNC_Prefs", Context.MODE_PRIVATE);
        String userName = prefs.getString("user_name", "");
        if (!userName.isEmpty()) {
            userNameTextView.setText(userName);
        }
        
        // Handle navigation item clicks and close drawer
        navigationView.setNavigationItemSelectedListener(item -> {
            if (item.getItemId() == R.id.nav_logout) {
                // Stop periodic location updates
                stopPeriodicLocationUpdates();
                // Send logout attendance and navigate to LoginActivity
                sendLogoutAttendance();
                Intent intent = new Intent(MainActivity.this, LoginActivity.class);
                intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
                startActivity(intent);
                finish();
                drawer.closeDrawer(navigationView);
                return true;
            }
            // For other items, use default navigation and close drawer
            boolean handled = NavigationUI.onNavDestinationSelected(item, navController);
            if (handled) {
                drawer.closeDrawer(navigationView);
            }
            return handled;
        });
        
        // Start periodic location updates (every 5 minutes)
        startPeriodicLocationUpdates();
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Menu removed - no options menu displayed
        return false;
    }

    @Override
    public boolean onSupportNavigateUp() {
        NavController navController = Navigation.findNavController(this, R.id.nav_host_fragment_content_main);
        return NavigationUI.navigateUp(navController, mAppBarConfiguration)
                || super.onSupportNavigateUp();
    }

    private void refreshHomeFragment() {
        // Find HomeFragment and refresh it
        if (navController != null && navController.getCurrentDestination() != null) {
            int currentDestinationId = navController.getCurrentDestination().getId();
            
            // Only refresh if we're on the home fragment
            if (currentDestinationId == R.id.nav_home) {
                androidx.fragment.app.Fragment navHostFragment = getSupportFragmentManager()
                        .findFragmentById(R.id.nav_host_fragment_content_main);
                
                if (navHostFragment != null && navHostFragment.getChildFragmentManager() != null) {
                    androidx.fragment.app.Fragment currentFragment = navHostFragment.getChildFragmentManager()
                            .getFragments().isEmpty() ? null : navHostFragment.getChildFragmentManager().getFragments().get(0);
                    
                    if (currentFragment instanceof com.example.glnc.ui.home.HomeFragment) {
                        ((com.example.glnc.ui.home.HomeFragment) currentFragment).refreshDeliveries();
                    }
                }
            }
        }
    }

    @Override
    public void onBackPressed() {
        // Send logout attendance when user presses back
        sendLogoutAttendance();
        super.onBackPressed();
    }

    @Override
    protected void onResume() {
        super.onResume();
        // Resume periodic location updates if not already running
        if (locationUpdateHandler == null) {
            startPeriodicLocationUpdates();
        }
    }
    
    @Override
    protected void onPause() {
        super.onPause();
        // Don't stop location updates on pause - keep them running in background
    }
    
    @Override
    protected void onDestroy() {
        // Stop periodic location updates
        stopPeriodicLocationUpdates();
        
        // Send logout attendance when activity is destroyed
        if (!isLoggingOut) {
            sendLogoutAttendance();
        }
        super.onDestroy();
    }

    private void sendLogoutAttendance() {
        if (isLoggingOut) {
            return; // Prevent duplicate calls
        }
        isLoggingOut = true;

        // Get stored user_id
        SharedPreferences prefs = getSharedPreferences("GLNC_Prefs", Context.MODE_PRIVATE);
        String userId = prefs.getString("user_id", "");

        if (userId.isEmpty()) {
            return; // No user logged in
        }

        // Get current location for logout using Global.getCurrentLocation for better accuracy
        Global.getCurrentLocation(this, new Global.LocationCallback() {
            @Override
            public void onLocationReceived(double latitude, double longitude, double altitude) {
                Log.d("MainActivity", "=== LOGOUT - LOCATION RECEIVED ===");

                // Send type 2 attendance (logout) with accurate GPS location
                sendAttendanceData(userId, latitude, longitude, altitude, 2);
            }

            @Override
            public void onLocationError(String error) {
                // Use stored location as fallback
                SharedPreferences prefs = getSharedPreferences("GLNC_Prefs", Context.MODE_PRIVATE);
                double latitude = prefs.getFloat("latitude", 0.0f);
                double longitude = prefs.getFloat("longitude", 0.0f);
                double altitude = prefs.getFloat("altitude", 0.0f);

                Log.d("MainActivity", "================================");
                sendAttendanceData(userId, latitude, longitude, altitude, 2);
            }
        });
    }

    private void sendAttendanceData(String userId, double latitude, double longitude, double altitude, int type) {
        new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    // Get current time
                    SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss", Locale.getDefault());
                    String currentTime = sdf.format(new Date());

                    // Create JSON body with format: (time, lati, longi, alti, type, user_id)
                    JSONObject jsonBody = new JSONObject();
                    jsonBody.put("time", currentTime);
                    jsonBody.put("lati", latitude);
                    jsonBody.put("longi", longitude);
                    jsonBody.put("alti", altitude);
                    jsonBody.put("type", type);
                    jsonBody.put("user_id", userId);
                    

                    Log.d("MainActivity", "========================================");

                    RequestBody body = RequestBody.create(
                            jsonBody.toString(),
                            MediaType.parse("application/json; charset=utf-8")
                    );

                    // Build request
                    Request request = new Request.Builder()
                            .url(global.serverUrl + "/app/excel/pointer")
                            .post(body)
                            .addHeader("Content-Type", "application/json")
                            .build();

                    // Execute request asynchronously
                    httpClient.newCall(request).enqueue(new Callback() {
                        @Override
                        public void onFailure(Call call, IOException e) {
                            Log.e("Attendance", "Failed to send attendance type " + type + ": " + e.getMessage());
                        }

                        @Override
                        public void onResponse(Call call, Response response) throws IOException {
                            final String responseBody = response.body() != null ? response.body().string() : "";
                            if (response.isSuccessful()) {

                                Log.d("MainActivity", "==================================================");
                            } else {

                                Log.e("MainActivity", "=============================================");
                            }
                        }
                    });
                } catch (Exception e) {
                    Log.e("Attendance", "Error sending attendance type " + type + ": " + e.getMessage());
                }
            }
        }).start();
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode == 1001 && resultCode == RESULT_OK) {
            // Sign activity completed successfully, refresh the home fragment
            refreshHomeFragment();
        }
    }
    
    private void startPeriodicLocationUpdates() {
        // Get user_id to verify user is logged in
        SharedPreferences prefs = getSharedPreferences("GLNC_Prefs", Context.MODE_PRIVATE);
        String userId = prefs.getString("user_id", "");
        
        if (userId.isEmpty()) {
            Log.w("MainActivity", "No user logged in, skipping periodic location updates");
            return;
        }
        
        // Stop any existing updates
        stopPeriodicLocationUpdates();
        
        // Create handler on main thread
        locationUpdateHandler = new Handler(Looper.getMainLooper());
        
        // Create runnable to send location periodically
        locationUpdateRunnable = new Runnable() {
            @Override
            public void run() {
                // Get current user_id (in case it changed)
                SharedPreferences prefs = getSharedPreferences("GLNC_Prefs", Context.MODE_PRIVATE);
                String userId = prefs.getString("user_id", "");
                
                if (userId.isEmpty()) {
                    Log.w("MainActivity", "User logged out, stopping periodic location updates");
                    stopPeriodicLocationUpdates();
                    return;
                }
                
                // Get current location and send it
                sendCurrentLocationToBackend(userId);
                
                // Schedule next update
                if (locationUpdateHandler != null) {
                    locationUpdateHandler.postDelayed(this, LOCATION_UPDATE_INTERVAL);
                }
            }
        };
        
        // Send first location immediately, then schedule periodic updates
        locationUpdateHandler.post(locationUpdateRunnable);
        

        Log.d("MainActivity", "==========================================");
    }
    
    private void stopPeriodicLocationUpdates() {
        if (locationUpdateHandler != null && locationUpdateRunnable != null) {
            locationUpdateHandler.removeCallbacks(locationUpdateRunnable);
            locationUpdateHandler = null;
            locationUpdateRunnable = null;
        }
    }
    
    private void sendCurrentLocationToBackend(String userId) {

        // Get current location using GPS
        Global.getCurrentLocation(this, new Global.LocationCallback() {
            @Override
            public void onLocationReceived(double latitude, double longitude, double altitude) {

                Log.d("MainActivity", "Sending to backend: /app/current_location");
                
                // Send location to backend
                sendLocationToBackend(userId, latitude, longitude, altitude);
            }
            
            @Override
            public void onLocationError(String error) {

                
                // Use stored location as fallback
                SharedPreferences prefs = getSharedPreferences("GLNC_Prefs", Context.MODE_PRIVATE);
                double latitude = prefs.getFloat("latitude", 0.0f);
                double longitude = prefs.getFloat("longitude", 0.0f);
                double altitude = prefs.getFloat("altitude", 0.0f);
                
                if (latitude != 0.0 || longitude != 0.0) {
                    Log.d("MainActivity", "Fallback Altitude: " + altitude);
                    sendLocationToBackend(userId, latitude, longitude, altitude);
                } else {
                    Log.e("MainActivity", "No location available (GPS failed and no stored location)");
                }
            }
        });
    }
    
    private void sendLocationToBackend(String userId, double latitude, double longitude, double altitude) {
        new Thread(() -> {
            try {
                // Create JSON body with user_id, latitude, longitude, altitude
                JSONObject jsonBody = new JSONObject();
                jsonBody.put("user_id", userId);
                jsonBody.put("latitude", latitude);
                jsonBody.put("longitude", longitude);
                jsonBody.put("altitude", altitude);
                

                Log.d("MainActivity", "=================================");
                
                RequestBody body = RequestBody.create(
                        jsonBody.toString(),
                        MediaType.parse("application/json; charset=utf-8")
                );
                
                // Build request
                Request request = new Request.Builder()
                        .url(global.serverUrl + "/app/current_location")
                        .post(body)
                        .addHeader("Content-Type", "application/json")
                        .build();
                
                // Execute request asynchronously
                httpClient.newCall(request).enqueue(new Callback() {
                    @Override
                    public void onFailure(Call call, IOException e) {
                        Log.e("MainActivity", "=== CURRENT LOCATION SEND FAILED ===");
                        Log.e("MainActivity", "Error: " + e.getMessage());
                        Log.e("MainActivity", "====================================");
                    }
                    
                    @Override
                    public void onResponse(Call call, Response response) throws IOException {
                        final String responseBody = response.body() != null ? response.body().string() : "";
                        if (response.isSuccessful()) {

                            Log.d("MainActivity", "==========================================");
                        } else {

                            Log.e("MainActivity", "====================================");
                        }
                    }
                });
            } catch (Exception e) {
                Log.e("MainActivity", "Error sending current location: " + e.getMessage());
            }
        }).start();
    }
}