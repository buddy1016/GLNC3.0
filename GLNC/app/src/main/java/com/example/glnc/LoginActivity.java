package com.example.glnc;

import android.Manifest;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
// Note: Using custom Location class (com.example.glnc.Location), not android.location.Location
// Use fully qualified name android.location.Location when needed for Android SDK Location objects
import android.nfc.Tag;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.appcompat.app.AlertDialog;
import androidx.appcompat.app.AppCompatActivity;
import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;


import com.example.glnc.databinding.ActivityLoginBinding;

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

public class LoginActivity extends AppCompatActivity {

    private ActivityLoginBinding binding;
    private StringBuilder accessCode = new StringBuilder();
    private Handler resetHandler = new Handler(Looper.getMainLooper());
    private Runnable resetRunnable;
    private static final int MAX_DIGITS = 5;
    private static final long RESET_DELAY_MS = 2500; // 2.5 seconds
    private static final int LOCATION_PERMISSION_REQUEST_CODE = 1001;
    private OkHttpClient httpClient;
    private Global global = new Global();
    // Note: Don't create separate Location instance - use Global.getLocation() instead
    // MainActivity will handle continuous GPS tracking

    // Store current location data
    private double currentLatitude = 0.0;
    private double currentLongitude = 0.0;
    private double currentAltitude = 0.0;
    private boolean hasLocation = false;
    private boolean pendingAttendanceData = false;
    private String pendingUserId = "";

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        binding = ActivityLoginBinding.inflate(getLayoutInflater());
        setContentView(binding.getRoot());

        // Initialize OkHttpClient
        httpClient = new OkHttpClient.Builder()
                .connectTimeout(10, TimeUnit.SECONDS)
                .readTimeout(10, TimeUnit.SECONDS)
                .writeTimeout(10, TimeUnit.SECONDS)
                .build();

        // Update time display
//        updateTime();
        new Handler(Looper.getMainLooper()).postDelayed(new Runnable() {
            @Override
            public void run() {
//                updateTime();
                new Handler(Looper.getMainLooper()).postDelayed(this, 60000); // Update every minute
            }
        }, 60000);

        // Setup number buttons
        setupButton(binding.btn0, "0");
        setupButton(binding.btn1, "1");
        setupButton(binding.btn2, "2");
        setupButton(binding.btn3, "3");
        setupButton(binding.btn4, "4");
        setupButton(binding.btn5, "5");
        setupButton(binding.btn6, "6");
        setupButton(binding.btn7, "7");
        setupButton(binding.btn8, "8");
        setupButton(binding.btn9, "9");

        // Setup clear button
        binding.btnClear.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                resetInput();
            }
        });

        // Initialize reset runnable
        resetRunnable = new Runnable() {
            @Override
            public void run() {
                resetInput();
            }
        };

        // Initialize access code display
        updateAccessCodeDisplay();

        // Request location permission and get location
        // Note: Don't create Location instance here - use Global.getLocation() instead
        // MainActivity will handle continuous GPS tracking
        requestLocationPermissionAndGetLocation();
    }

    private void setupButton(Button button, String digit) {
        button.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                handleDigitInput(digit);
            }
        });
    }

    private void handleDigitInput(String digit) {
        // Cancel any pending reset
        resetHandler.removeCallbacks(resetRunnable);

        // Only allow digits if we haven't reached max
        if (accessCode.length() < MAX_DIGITS) {
            accessCode.append(digit);
            updateAccessCodeDisplay();

            // If we've reached 5 digits, automatically send login request
            if (accessCode.length() == MAX_DIGITS) {
                sendLoginRequest();
                // Schedule reset after 250ms if no action is taken
                resetHandler.postDelayed(resetRunnable, RESET_DELAY_MS);
            } else {
                // Schedule reset after 250ms of inactivity
                resetHandler.postDelayed(resetRunnable, RESET_DELAY_MS);
            }
        }
    }

    private void resetInput() {
        accessCode.setLength(0);
        updateAccessCodeDisplay();
        resetHandler.removeCallbacks(resetRunnable);
    }

    private void updateAccessCodeDisplay() {
        // Display asterisks for each entered digit
        StringBuilder display = new StringBuilder();
        for (int i = 0; i < accessCode.length(); i++) {
            display.append("*");
        }
        binding.accessCodeDisplay.setText(display.toString());
    }

    private void sendLoginRequest() {
        String code = accessCode.toString();

        // Send request asynchronously to avoid freezing
        new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    // Create JSON body
                    JSONObject jsonBody = new JSONObject();
                    jsonBody.put("code", code);

                    RequestBody body = RequestBody.create(
                            jsonBody.toString(),
                            MediaType.parse("application/json; charset=utf-8")
                    );

                    // Build request
                    Request request = new Request.Builder()
                            .url(global.serverUrl + "/app/login")
                            .post(body)
                            .addHeader("Content-Type", "application/json")
                            .build();

                    // Execute request asynchronously
                    httpClient.newCall(request).enqueue(new Callback() {
                        @Override
                        public void onFailure(Call call, IOException e) {
                            // Handle error on main thread
                            runOnUiThread(new Runnable() {
                                @Override
                                public void run() {
                                    Toast.makeText(LoginActivity.this,
                                            "Login failed: " + e.getMessage(),
                                            Toast.LENGTH_SHORT).show();
                                }
                            });
                        }

                        @Override
                        public void onResponse(Call call, Response response) throws IOException {
                            final String responseBody = response.body() != null ?
                                    response.body().string() : "";

                            // Handle response on main thread
                            runOnUiThread(new Runnable() {
                                @Override
                                public void run() {
                                    if (response.isSuccessful()) {
                                        try {
                                            // Parse user information from login response
                                            JSONObject responseJson = new JSONObject(responseBody);

                                            // Try different possible field names for user_id and user_name
                                            String userId = "";
                                            String userName = "";

                                            if (responseJson.has("user_id")) {
                                                userId = responseJson.optString("user_id", "");
                                            } else if (responseJson.has("userId")) {
                                                userId = responseJson.optString("userId", "");
                                            } else if (responseJson.has("id")) {
                                                userId = String.valueOf(responseJson.optInt("id", 0));
                                            } else if (responseJson.has("user")) {
                                                JSONObject userObj = responseJson.optJSONObject("user");
                                                if (userObj != null) {
                                                    userId = userObj.optString("id", "");
                                                    if (userId.isEmpty()) {
                                                        userId = userObj.optString("user_id", "");
                                                    }
                                                }
                                            }

                                            // Try different possible field names for user name
                                            if (responseJson.has("name")) {
                                                userName = responseJson.optString("name", "");
                                            } else if (responseJson.has("user_name")) {
                                                userName = responseJson.optString("user_name", "");
                                            } else if (responseJson.has("username")) {
                                                userName = responseJson.optString("username", "");
                                            } else if (responseJson.has("user")) {
                                                JSONObject userObj = responseJson.optJSONObject("user");
                                                if (userObj != null) {
                                                    userName = userObj.optString("name", "");
                                                    if (userName.isEmpty()) {
                                                        userName = userObj.optString("user_name", "");
                                                    }
                                                    if (userName.isEmpty()) {
                                                        userName = userObj.optString("username", "");
                                                    }
                                                }
                                            }

                                            if (!userId.isEmpty()) {
                                                // Store user_id, user_name and location for logout use
                                                storeUserData(userId, userName);

                                                // Send attendance data type 1 (login)
                                                sendAttendanceData(userId, 1);
                                            }

                                            // Navigate to MainActivity (home screen)
                                            Intent intent = new Intent(LoginActivity.this, MainActivity.class);
                                            startActivity(intent);
                                            finish(); // Close login activity
                                        } catch (Exception e) {
                                            // If parsing fails, still navigate but log error
                                            android.util.Log.e("Login", "Failed to parse login response: " + e.getMessage());
                                            Intent intent = new Intent(LoginActivity.this, MainActivity.class);
                                            startActivity(intent);
                                            finish();
                                        }
                                    } else {
                                        Toast.makeText(LoginActivity.this,
                                                "Login failed: " + responseBody,
                                                Toast.LENGTH_SHORT).show();
                                    }
                                }
                            });
                        }
                    });
                } catch (Exception e) {
                    runOnUiThread(new Runnable() {
                        @Override
                        public void run() {
                            Toast.makeText(LoginActivity.this,
                                    "Error: " + e.getMessage(),
                                    Toast.LENGTH_SHORT).show();
                        }
                    });
                }
            }
        }).start();
    }

//    private void updateTime() {
//        SimpleDateFormat sdf = new SimpleDateFormat("HH:mm", Locale.getDefault());
//        String currentTime = sdf.format(new Date());
//        binding.timeTextView.setText(currentTime);
//    }

    private void requestLocationPermissionAndGetLocation() {
        // Check if location permission is granted
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION)
                != PackageManager.PERMISSION_GRANTED) {
            // Request permission
            ActivityCompat.requestPermissions(this,
                    new String[]{Manifest.permission.ACCESS_FINE_LOCATION},
                    LOCATION_PERMISSION_REQUEST_CODE);
        } else {
            // Permission already granted, get location
            getCurrentLocation();
        }
    }

    @Override
    public void onRequestPermissionsResult(int requestCode, @NonNull String[] permissions,
                                           @NonNull int[] grantResults) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults);
        if (requestCode == LOCATION_PERMISSION_REQUEST_CODE) {
            if (grantResults.length > 0 && grantResults[0] == PackageManager.PERMISSION_GRANTED) {
                // Permission granted, get location
                getCurrentLocation();
            }
            // Permission denied - silently continue without location
        }
    }

    private void getCurrentLocation() {
        if (ActivityCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION)
                != PackageManager.PERMISSION_GRANTED) {
            return;
        }

        // Get location from Global storage (set by MainActivity's Location instance)
        // LoginActivity doesn't create its own Location instance to avoid conflicts
        android.location.Location storedLocation = Global.getLocation();
        if (storedLocation != null) {
            currentLatitude = storedLocation.getLatitude();
            currentLongitude = storedLocation.getLongitude();
            currentAltitude = storedLocation.getAltitude();
            hasLocation = true;
            Log.d("LoginActivity", "Location obtained from Global storage: " + currentLatitude + ", " + currentLongitude);
            
            // If we have pending attendance data to send, send it now
            if (pendingAttendanceData && !pendingUserId.isEmpty()) {
                sendAttendanceDataInternal(pendingUserId, 1);
                pendingAttendanceData = false;
                pendingUserId = "";
            }
        } else {
            // No location in Global storage yet - try SharedPreferences as fallback
            SharedPreferences prefs = getSharedPreferences("GLNC_Prefs", Context.MODE_PRIVATE);
            float lat = prefs.getFloat("latitude", 0.0f);
            float lon = prefs.getFloat("longitude", 0.0f);
            float alt = prefs.getFloat("altitude", 0.0f);
            
            if (lat != 0.0f || lon != 0.0f) {
                currentLatitude = lat;
                currentLongitude = lon;
                currentAltitude = alt;
                hasLocation = true;
                Log.d("LoginActivity", "Location obtained from SharedPreferences: " + currentLatitude + ", " + currentLongitude);
                
                // If we have pending attendance data to send, send it now
                if (pendingAttendanceData && !pendingUserId.isEmpty()) {
                    sendAttendanceDataInternal(pendingUserId, 1);
                    pendingAttendanceData = false;
                    pendingUserId = "";
                }
            } else {
                // No location available yet - wait for MainActivity to initialize GPS
                Log.w("LoginActivity", "No location available yet. MainActivity will initialize GPS tracking.");
                hasLocation = false;
            }
        }
    }

    private void showLocationAlert(String title, String message) {
        new AlertDialog.Builder(this)
                .setTitle(title)
                .setMessage(message)
                .setPositiveButton("OK", null)
                .setCancelable(true)
                .show();
    }

    private void storeUserData(String userId, String userName) {
        // Store user_id, user_name and location data in SharedPreferences for logout use
        SharedPreferences prefs = getSharedPreferences("GLNC_Prefs", Context.MODE_PRIVATE);
        SharedPreferences.Editor editor = prefs.edit();
        editor.putString("user_id", userId);
        editor.putString("user_name", userName);
        editor.putFloat("latitude", (float) currentLatitude);
        editor.putFloat("longitude", (float) currentLongitude);
        editor.putFloat("altitude", (float) currentAltitude);
        editor.putBoolean("has_location", hasLocation);
        editor.apply();
    }

    private void sendAttendanceData(String userId, int type) {

        Log.d("LoginActivity", "Current Coordinates: " + currentLatitude + ", " + currentLongitude);

        // Ensure we have location data
        if (!hasLocation || (currentLatitude == 0.0 && currentLongitude == 0.0)) {
            // Store as pending to send when location is received
            pendingAttendanceData = true;
            pendingUserId = userId;

            // Try to get location again if not available
            getCurrentLocation();

            // Also set up a delayed check as backup (in case location callback doesn't fire)
            new Handler(Looper.getMainLooper()).postDelayed(new Runnable() {
                @Override
                public void run() {
                    if (hasLocation && pendingAttendanceData && !pendingUserId.isEmpty()) {
                        sendAttendanceDataInternal(pendingUserId, type);
                        pendingAttendanceData = false;
                        pendingUserId = "";
                    } else if (!hasLocation) {
                        Log.w("LoginActivity", "Location still not available after delay, will retry when location is received");
                    }
                }
            }, 2000); // Wait 2 seconds
            return;
        }

        sendAttendanceDataInternal(userId, type);
    }

    private void sendAttendanceDataInternal(String userId, int type) {

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
                    jsonBody.put("lati", currentLatitude);
                    jsonBody.put("longi", currentLongitude);
                    jsonBody.put("alti", currentAltitude);
                    jsonBody.put("type", type);
                    jsonBody.put("user_id", userId);


                    Log.d("LoginActivity", "=========================================");


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
                            Log.e("LoginActivity", "=== ATTENDANCE DATA SEND FAILED (SIGN UP) ===");
                            Log.e("LoginActivity", "Error: " + e.getMessage());
                            Log.e("LoginActivity", "=============================================");
                        }

                        @Override
                        public void onResponse(Call call, Response response) throws IOException {
                            final String responseBody = response.body() != null ? response.body().string() : "";
                            if (response.isSuccessful()) {

                                Log.d("LoginActivity", "==================================================");
                            } else {

                                Log.e("LoginActivity", "=============================================");
                            }
                        }
                    });
                } catch (Exception e) {
                    android.util.Log.e("Attendance", "Error sending attendance type " + type + ": " + e.getMessage());
                }
            }
        }).start();
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        if (resetHandler != null) {
            resetHandler.removeCallbacks(resetRunnable);
        }
        // Note: No Location instance to stop - MainActivity handles GPS tracking
    }
}