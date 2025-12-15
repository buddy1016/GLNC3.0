package com.example.glnc.ui.home;

import android.app.Dialog;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.content.Context;
import android.content.Intent;
import android.os.Build;
import android.os.Bundle;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;
import android.widget.Button;
import android.widget.ProgressBar;
import android.widget.TextView;
import android.widget.Toast;

import androidx.core.app.NotificationCompat;

import androidx.annotation.NonNull;
import androidx.fragment.app.Fragment;
import androidx.lifecycle.ViewModelProvider;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.example.glnc.Global;
import com.example.glnc.R;
import com.example.glnc.databinding.DialogDeliveryDetailsBinding;
import com.example.glnc.databinding.FragmentHomeBinding;

import org.json.JSONObject;

import java.io.IOException;
import java.util.List;
import java.util.concurrent.TimeUnit;

import okhttp3.Call;
import okhttp3.Callback;
import okhttp3.MediaType;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.RequestBody;
import okhttp3.Response;

public class HomeFragment extends Fragment {

    private FragmentHomeBinding binding;
    private HomeViewModel homeViewModel;
    private DeliveryAdapter deliveryAdapter;
    private OkHttpClient httpClient;
    private Global global = new Global();

    public View onCreateView(@NonNull LayoutInflater inflater,
                             ViewGroup container, Bundle savedInstanceState) {
        homeViewModel = new ViewModelProvider(this).get(HomeViewModel.class);

        binding = FragmentHomeBinding.inflate(inflater, container, false);
        View root = binding.getRoot();

        // Initialize OkHttpClient
        httpClient = new OkHttpClient.Builder()
                .connectTimeout(10, TimeUnit.SECONDS)
                .readTimeout(10, TimeUnit.SECONDS)
                .writeTimeout(10, TimeUnit.SECONDS)
                .build();

        // Setup RecyclerView
        RecyclerView recyclerView = binding.deliveryRecyclerView;
        recyclerView.setLayoutManager(new LinearLayoutManager(getContext()));
        deliveryAdapter = new DeliveryAdapter();
        recyclerView.setAdapter(deliveryAdapter);

        // Setup item click listener to show details dialog
        deliveryAdapter.setOnItemClickListener(delivery -> {
            showDeliveryDetailsDialog(delivery);
        });

        // Observe deliveries
        homeViewModel.getDeliveries().observe(getViewLifecycleOwner(), deliveries -> {
            if (deliveries != null && !deliveries.isEmpty()) {
                deliveryAdapter.setDeliveries(deliveries);
                binding.emptyStateText.setVisibility(View.GONE);
                binding.deliveryRecyclerView.setVisibility(View.VISIBLE);
            } else {
                binding.emptyStateText.setVisibility(View.VISIBLE);
                binding.deliveryRecyclerView.setVisibility(View.GONE);
            }
        });

        // Observe loading state
        homeViewModel.isLoading().observe(getViewLifecycleOwner(), isLoading -> {
            if (isLoading) {
                binding.loadingProgress.setVisibility(View.VISIBLE);
                binding.deliveryRecyclerView.setVisibility(View.GONE);
                binding.emptyStateText.setVisibility(View.GONE);
            } else {
                binding.loadingProgress.setVisibility(View.GONE);
            }
        });

        // Observe errors
        homeViewModel.getError().observe(getViewLifecycleOwner(), error -> {
            if (error != null) {
                Toast.makeText(getContext(), error, Toast.LENGTH_SHORT).show();
            }
        });

        // Fetch deliveries when fragment is created
        if (getContext() != null) {
            homeViewModel.fetchDeliveries(getContext());
        }

        return root;
    }

    private void showDeliveryDetailsDialog(Delivery delivery) {
        Dialog dialog = new Dialog(requireContext());
        dialog.requestWindowFeature(Window.FEATURE_NO_TITLE);
        
        DialogDeliveryDetailsBinding dialogBinding = DialogDeliveryDetailsBinding.inflate(LayoutInflater.from(requireContext()));
        dialog.setContentView(dialogBinding.getRoot());
        
        // Set delivery details
        dialogBinding.detailTime.setText(delivery.getTime());
        dialogBinding.detailClient.setText(delivery.getClient() != null ? delivery.getClient() : "");
        dialogBinding.detailAddress.setText(delivery.getAddress() != null ? delivery.getAddress() : "N/A");
        dialogBinding.detailContact.setText(delivery.getContact() != null ? delivery.getContact() : "N/A");
        dialogBinding.detailDetail.setText(delivery.getDetail() != null ? delivery.getDetail() : "N/A");
        
        // Only show action buttons for in-progress deliveries
        // Cancelled or completed deliveries require no action
        if (delivery.isInProgress()) {
            // Show buttons for in-progress deliveries
            dialogBinding.returnButton.setVisibility(View.VISIBLE);
            dialogBinding.signButton.setVisibility(View.VISIBLE);
            
            // Setup Return button
            dialogBinding.returnButton.setOnClickListener(v -> {
                dialog.dismiss();
                sendCancelDeliveryRequest(delivery);
            });
            
            // Setup Sign button
            dialogBinding.signButton.setOnClickListener(v -> {
                dialog.dismiss();
                // Open SignActivity and wait for result
                Intent signIntent = new Intent(getContext(), com.example.glnc.SignActivity.class);
                signIntent.putExtra("delivery_id", delivery.getId());
                requireActivity().startActivityForResult(signIntent, 1001); // Request code for sign activity
            });
        } else {
            // Hide buttons for cancelled or completed deliveries
            dialogBinding.returnButton.setVisibility(View.GONE);
            dialogBinding.signButton.setVisibility(View.GONE);
        }
        
        // Set dialog window properties
        Window window = dialog.getWindow();
        if (window != null) {
            window.setLayout(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT);
            window.setBackgroundDrawableResource(android.R.color.transparent);
        }
        
        dialog.show();
    }

    public void refreshDeliveries() {
        if (getContext() != null && homeViewModel != null) {
            homeViewModel.fetchDeliveries(getContext());
        }
    }

    private void showCancellationNotification(Delivery delivery) {
        Context context = getContext();
        if (context == null) return;

        // Create notification channel for Android O and above
        String channelId = "delivery_cancellation_channel";
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationChannel channel = new NotificationChannel(
                    channelId,
                    "Delivery Cancellation",
                    NotificationManager.IMPORTANCE_HIGH
            );
            channel.setDescription("Notifications for delivery cancellations");
            NotificationManager notificationManager = context.getSystemService(NotificationManager.class);
            if (notificationManager != null) {
                notificationManager.createNotificationChannel(channel);
            }
        }

        // Build notification
        NotificationCompat.Builder builder = new NotificationCompat.Builder(context, channelId)
                .setSmallIcon(android.R.drawable.ic_dialog_info)
                .setContentTitle("Delivery Cancelled")
                .setContentText("Delivery to " + (delivery.getClient() != null ? delivery.getClient() : "client") + " has been cancelled")
                .setStyle(new NotificationCompat.BigTextStyle()
                        .bigText("Delivery to " + (delivery.getClient() != null ? delivery.getClient() : "client") + 
                                " scheduled for " + (delivery.getTime() != null ? delivery.getTime() : "") + 
                                " has been cancelled."))
                .setPriority(NotificationCompat.PRIORITY_HIGH)
                .setAutoCancel(true);

        // Show notification
        NotificationManager notificationManager = (NotificationManager) context.getSystemService(Context.NOTIFICATION_SERVICE);
        if (notificationManager != null) {
            notificationManager.notify((int) System.currentTimeMillis(), builder.build());
        }
    }

    private void sendCancelDeliveryRequest(Delivery delivery) {
        if (delivery.getId() == null || delivery.getId().isEmpty()) {
            Toast.makeText(getContext(), "Invalid delivery ID", Toast.LENGTH_SHORT).show();
            return;
        }

        new Thread(() -> {
            try {
                // Create JSON body with delivery id
                JSONObject jsonBody = new JSONObject();
                jsonBody.put("id", delivery.getId());

                RequestBody body = RequestBody.create(
                        jsonBody.toString(),
                        MediaType.parse("application/json; charset=utf-8")
                );

                // Build request
                Request request = new Request.Builder()
                        .url(global.serverUrl + "/app/delivery_cancel")
                        .post(body)
                        .addHeader("Content-Type", "application/json")
                        .build();

                // Execute request asynchronously
                httpClient.newCall(request).enqueue(new Callback() {
                    @Override
                    public void onFailure(Call call, IOException e) {
                        requireActivity().runOnUiThread(() -> {
                            Toast.makeText(getContext(), 
                                    "Failed to cancel delivery: " + e.getMessage(), 
                                    Toast.LENGTH_SHORT).show();
                            Log.e("HomeFragment", "Failed to cancel delivery", e);
                        });
                    }

                    @Override
                    public void onResponse(Call call, Response response) throws IOException {
                        requireActivity().runOnUiThread(() -> {
                            if (response.isSuccessful()) {
                                // Show notification to driver
                                showCancellationNotification(delivery);
                                
                                Toast.makeText(getContext(), 
                                        "Delivery cancelled successfully", 
                                        Toast.LENGTH_SHORT).show();
                                // Refresh the delivery list
                                refreshDeliveries();
                            } else {
                                String responseBody = null;
                                try {
                                    responseBody = response.body() != null ?
                                            response.body().string() : "";
                                } catch (IOException e) {
                                    throw new RuntimeException(e);
                                }
                                Toast.makeText(getContext(), 
                                        "Failed to cancel delivery: " + response.code(), 
                                        Toast.LENGTH_SHORT).show();
                                Log.e("HomeFragment", "Failed to cancel delivery: " + responseBody);
                            }
                        });
                    }
                });
            } catch (Exception e) {
                requireActivity().runOnUiThread(() -> {
                    Toast.makeText(getContext(), 
                            "Error: " + e.getMessage(), 
                            Toast.LENGTH_SHORT).show();
                    Log.e("HomeFragment", "Error cancelling delivery", e);
                });
            }
        }).start();
    }

    @Override
    public void onResume() {
        super.onResume();
        // Refresh deliveries when fragment resumes (in case sign activity completed)
        // This ensures the list is updated even if activity result handling fails
        refreshDeliveries();
    }

    @Override
    public void onDestroyView() {
        super.onDestroyView();
        binding = null;
    }
}
