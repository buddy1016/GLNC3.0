package com.example.glnc.ui.home;

import android.content.Context;
import android.content.SharedPreferences;
import android.util.Log;

import androidx.lifecycle.LiveData;
import androidx.lifecycle.MutableLiveData;
import androidx.lifecycle.ViewModel;

import com.example.glnc.Global;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.IOException;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Comparator;
import java.util.List;
import java.util.concurrent.TimeUnit;

import okhttp3.Call;
import okhttp3.Callback;
import okhttp3.MediaType;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.Response;

public class HomeViewModel extends ViewModel {

    private MutableLiveData<List<Delivery>> deliveriesLiveData;
    private MutableLiveData<Boolean> isLoadingLiveData;
    private MutableLiveData<String> errorLiveData;
    private Global global = new Global();
    private OkHttpClient httpClient;

    public HomeViewModel() {
        deliveriesLiveData = new MutableLiveData<>();
        isLoadingLiveData = new MutableLiveData<>(false);
        errorLiveData = new MutableLiveData<>();
        
        httpClient = new OkHttpClient.Builder()
                .connectTimeout(10, TimeUnit.SECONDS)
                .readTimeout(10, TimeUnit.SECONDS)
                .writeTimeout(10, TimeUnit.SECONDS)
                .build();
    }

    public LiveData<List<Delivery>> getDeliveries() {
        return deliveriesLiveData;
    }

    public LiveData<Boolean> isLoading() {
        return isLoadingLiveData;
    }

    public LiveData<String> getError() {
        return errorLiveData;
    }

    public void fetchDeliveries(Context context) {
        isLoadingLiveData.setValue(true);
        errorLiveData.setValue(null);

        // Get user_id from SharedPreferences
        SharedPreferences prefs = context.getSharedPreferences("GLNC_Prefs", Context.MODE_PRIVATE);
        String userId = prefs.getString("user_id", "");

        if (userId.isEmpty()) {
            errorLiveData.setValue("User not logged in");
            isLoadingLiveData.setValue(false);
            return;
        }

        new Thread(() -> {
            try {
                // Create JSON body with user_id
                JSONObject jsonBody = new JSONObject();
                jsonBody.put("user_id", userId);

                okhttp3.RequestBody body = okhttp3.RequestBody.create(
                        jsonBody.toString(),
                        MediaType.parse("application/json; charset=utf-8")
                );

                // Build request
                Request request = new Request.Builder()
                        .url(global.serverUrl + "/app/delivery")
                        .post(body)
                        .addHeader("Content-Type", "application/json")
                        .build();

                // Execute request
                httpClient.newCall(request).enqueue(new Callback() {
                    @Override
                    public void onFailure(Call call, IOException e) {
                        isLoadingLiveData.postValue(false);
                        errorLiveData.postValue("Failed to fetch deliveries: " + e.getMessage());
                        Log.e("HomeViewModel", "Failed to fetch deliveries", e);
                    }

                    @Override
                    public void onResponse(Call call, Response response) throws IOException {
                        isLoadingLiveData.postValue(false);
                        
                        if (response.isSuccessful()) {
                            String responseBody = response.body() != null ? 
                                    response.body().string() : "";
                            
                            try {
                                List<Delivery> deliveries = parseDeliveries(responseBody);
                                
                                // Sort: in-progress first, then cancelled, then completed
                                Collections.sort(deliveries, new Comparator<Delivery>() {
                                    @Override
                                    public int compare(Delivery d1, Delivery d2) {
                                        // In-progress has highest priority
                                        if (d1.isInProgress() && !d2.isInProgress()) return -1;
                                        if (!d1.isInProgress() && d2.isInProgress()) return 1;
                                        
                                        // Cancelled comes after in-progress
                                        if (d1.isCancelled() && d2.isCompleted()) return -1;
                                        if (d1.isCompleted() && d2.isCancelled()) return 1;
                                        
                                        return 0;
                                    }
                                });
                                
                                deliveriesLiveData.postValue(deliveries);
                            } catch (Exception e) {
                                errorLiveData.postValue("Failed to parse deliveries: " + e.getMessage());
                                Log.e("HomeViewModel", "Failed to parse deliveries", e);
                            }
                        } else {
                            errorLiveData.postValue("Failed to fetch deliveries: " + response.code());
                        }
                    }
                });
            } catch (Exception e) {
                isLoadingLiveData.postValue(false);
                errorLiveData.postValue("Error: " + e.getMessage());
                Log.e("HomeViewModel", "Error fetching deliveries", e);
            }
        }).start();
    }

    private List<Delivery> parseDeliveries(String responseBody) throws Exception {
        List<Delivery> deliveries = new ArrayList<>();
        
        JSONArray deliveriesArray = null;
        
        // Try to parse as JSON array first
        try {
            deliveriesArray = new JSONArray(responseBody);
        } catch (Exception e) {
            // If not an array, try as JSON object
            try {
                JSONObject jsonResponse = new JSONObject(responseBody);
                
                // Try different response formats
                if (jsonResponse.has("deliveries")) {
                    deliveriesArray = jsonResponse.getJSONArray("deliveries");
                } else if (jsonResponse.has("data")) {
                    deliveriesArray = jsonResponse.getJSONArray("data");
                } else if (jsonResponse.has("items")) {
                    deliveriesArray = jsonResponse.getJSONArray("items");
                }
            } catch (Exception ex) {
                Log.e("HomeViewModel", "Failed to parse response", ex);
            }
        }
        
        if (deliveriesArray != null) {
            for (int i = 0; i < deliveriesArray.length(); i++) {
                JSONObject deliveryObj = deliveriesArray.getJSONObject(i);
                Delivery delivery = new Delivery();
                
                // Parse id
                if (deliveryObj.has("id")) {
                    delivery.setId(deliveryObj.getString("id"));
                } else if (deliveryObj.has("delivery_id")) {
                    delivery.setId(deliveryObj.getString("delivery_id"));
                }
                
                // Parse date_time_leave and extract time
                if (deliveryObj.has("date_time_leave")) {
                    String dateTimeLeave = deliveryObj.getString("date_time_leave");
                    // Extract time part (HH:mm) from "yyyy-MM-dd HH:mm:ss"
                    if (dateTimeLeave.length() >= 16) {
                        delivery.setTime(dateTimeLeave.substring(11, 16)); // Extract HH:mm
                    } else {
                        delivery.setTime(dateTimeLeave);
                    }
                } else if (deliveryObj.has("time")) {
                    delivery.setTime(deliveryObj.getString("time"));
                } else if (deliveryObj.has("delivery_time")) {
                    delivery.setTime(deliveryObj.getString("delivery_time"));
                } else if (deliveryObj.has("schedule_time")) {
                    delivery.setTime(deliveryObj.getString("schedule_time"));
                }
                
                // Parse client
                if (deliveryObj.has("client")) {
                    delivery.setClient(deliveryObj.getString("client"));
                } else if (deliveryObj.has("client_name")) {
                    delivery.setClient(deliveryObj.getString("client_name"));
                } else if (deliveryObj.has("customer")) {
                    delivery.setClient(deliveryObj.getString("customer"));
                }
                
                // Parse address
                if (deliveryObj.has("Address")) {
                    delivery.setAddress(deliveryObj.getString("Address"));
                } else if (deliveryObj.has("address")) {
                    delivery.setAddress(deliveryObj.getString("address"));
                }
                
                // Parse contact
                if (deliveryObj.has("Contact")) {
                    delivery.setContact(deliveryObj.getString("Contact"));
                } else if (deliveryObj.has("contact")) {
                    delivery.setContact(deliveryObj.getString("contact"));
                }
                
                // Parse detail
                if (deliveryObj.has("Detail")) {
                    delivery.setDetail(deliveryObj.getString("Detail"));
                } else if (deliveryObj.has("detail")) {
                    delivery.setDetail(deliveryObj.getString("detail"));
                } else if (deliveryObj.has("description")) {
                    delivery.setDetail(deliveryObj.getString("description"));
                }
                
                // Determine status based on date_time_arrival and return_flag
                // Backend format:
                // - return_flag: 1 (if ReturnFlag is true) or 0 (if ReturnFlag is false)
                // - date_time_arrival: "yyyy-MM-dd HH:mm:ss" or empty string "" if null
                // Logic:
                // - If return_flag is 1 → cancelled
                // - If return_flag is 0 AND date_time_arrival is not empty → completed
                // - Otherwise → in_progress
                String status = "in_progress"; // Default
                
                // Parse return_flag (backend sends as integer: 1 or 0)
                int returnFlag = -1;
                boolean returnFlagFound = false;
                
                if (deliveryObj.has("return_flag")) {
                    returnFlagFound = true;
                    try {
                        // Backend sends as integer (1 or 0)
                        returnFlag = deliveryObj.getInt("return_flag");
                    } catch (Exception e) {
                        // Fallback: try as string
                        try {
                            String strValue = deliveryObj.getString("return_flag");
                            returnFlag = Integer.parseInt(strValue);
                        } catch (Exception ex) {
                            Log.w("HomeViewModel", "Could not parse return_flag: " + ex.getMessage());
                        }
                    }
                }
                
                // Parse date_time_arrival (backend sends as "yyyy-MM-dd HH:mm:ss" or empty string "")
                String dateTimeArrival = "";
                if (deliveryObj.has("date_time_arrival")) {
                    dateTimeArrival = deliveryObj.optString("date_time_arrival", "");
                }
                
                // Determine status based on backend logic
                if (returnFlag == 1) {
                    // return_flag = 1 means cancelled
                    status = "cancelled";
                } else if (returnFlag == 0 && dateTimeArrival != null && !dateTimeArrival.trim().isEmpty()) {
                    // return_flag = 0 AND date_time_arrival is not empty means completed
                    status = "completed";
                } else {
                    // Otherwise, it's in progress
                    status = "in_progress";
                    if (returnFlagFound) {
                        Log.d("HomeViewModel", "Delivery ID: " + delivery.getId() + ", Client: " + delivery.getClient() + " -> IN PROGRESS (return_flag=" + returnFlag + ", arrival=" + (dateTimeArrival.isEmpty() ? "empty" : dateTimeArrival) + ")");
                    } else {
                        Log.w("HomeViewModel", "Delivery ID: " + delivery.getId() + ", Client: " + delivery.getClient() + " -> IN PROGRESS (return_flag field not found)");
                    }
                }
                
                delivery.setStatus(status);
                
                deliveries.add(delivery);
            }
        }
        
        return deliveries;
    }
}
