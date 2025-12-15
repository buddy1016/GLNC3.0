package com.example.glnc;

import android.Manifest;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.graphics.Bitmap;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.graphics.Path;
import android.net.Uri;
import android.os.Bundle;
import android.provider.MediaStore;
import android.util.Log;
import android.view.MotionEvent;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ImageView;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.appcompat.app.AppCompatActivity;
import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.util.concurrent.TimeUnit;

import okhttp3.Call;
import okhttp3.Callback;
import okhttp3.MediaType;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.RequestBody;
import okhttp3.Response;

import org.json.JSONObject;

public class SignActivity extends AppCompatActivity {

    private static final int CAMERA_PERMISSION_REQUEST = 100;
    private static final int CAMERA_CAPTURE_REQUEST = 101;
    private static final int GALLERY_PICK_REQUEST = 102;

    private ImageView photoPreview;
    private android.widget.FrameLayout signatureContainer;
    private SignatureView signatureView;
    private EditText commentInput;
    private EditText weightInput;
    private Button happyButton;
    private Button neutralButton;
    private Button sadButton;
    private Button submitButton;
    private Button cameraButton;
    private Button clearSignatureButton;

    private String deliveryId;
    private String selectedSatisfaction = null;
    private Uri photoUri;
    private OkHttpClient httpClient;
    private Global global = new Global();
    private android.app.ProgressDialog progressDialog;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_sign);

        // Get delivery ID from intent
        deliveryId = getIntent().getStringExtra("delivery_id");
        if (deliveryId == null || deliveryId.isEmpty()) {
            Toast.makeText(this, "Invalid delivery ID", Toast.LENGTH_SHORT).show();
            finish();
            return;
        }

        // Initialize OkHttpClient
        httpClient = new OkHttpClient.Builder()
                .connectTimeout(30, TimeUnit.SECONDS)
                .readTimeout(30, TimeUnit.SECONDS)
                .writeTimeout(30, TimeUnit.SECONDS)
                .build();

        // Initialize views
        photoPreview = findViewById(R.id.photo_preview);
        signatureContainer = findViewById(R.id.signature_container);
        signatureView = new SignatureView(this);
        signatureContainer.addView(signatureView, 0, new android.widget.FrameLayout.LayoutParams(
            android.widget.FrameLayout.LayoutParams.MATCH_PARENT,
            android.widget.FrameLayout.LayoutParams.MATCH_PARENT));
        commentInput = findViewById(R.id.comment_input);
        weightInput = findViewById(R.id.weight_input);
        happyButton = findViewById(R.id.happy_button);
        neutralButton = findViewById(R.id.neutral_button);
        sadButton = findViewById(R.id.sad_button);
        submitButton = findViewById(R.id.submit_button);
        cameraButton = findViewById(R.id.camera_button);
        clearSignatureButton = findViewById(R.id.clear_signature_button);

        // Setup camera button - show options for camera or gallery
        cameraButton.setOnClickListener(v -> showImageSourceDialog());

        // Setup clear signature button
        clearSignatureButton.setOnClickListener(v -> {
            signatureView.clear();
            updateSubmitButtonState();
        });

        // Setup satisfaction buttons
        happyButton.setOnClickListener(v -> selectSatisfaction("happy", happyButton));
        neutralButton.setOnClickListener(v -> selectSatisfaction("neutral", neutralButton));
        sadButton.setOnClickListener(v -> selectSatisfaction("sad", sadButton));

        // Setup submit button
        submitButton.setOnClickListener(v -> submitDelivery());

        // Monitor signature drawing
        signatureView.setOnSignatureChangeListener(() -> updateSubmitButtonState());

        // Monitor input changes
        commentInput.addTextChangedListener(new android.text.TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence s, int start, int count, int after) {}

            @Override
            public void onTextChanged(CharSequence s, int start, int before, int count) {}

            @Override
            public void afterTextChanged(android.text.Editable s) {
                updateSubmitButtonState();
            }
        });

        weightInput.addTextChangedListener(new android.text.TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence s, int start, int count, int after) {}

            @Override
            public void onTextChanged(CharSequence s, int start, int before, int count) {}

            @Override
            public void afterTextChanged(android.text.Editable s) {
                updateSubmitButtonState();
            }
        });

        updateSubmitButtonState();
    }

    private boolean checkCameraPermission() {
        return ContextCompat.checkSelfPermission(this, Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED;
    }

    private void requestCameraPermission() {
        ActivityCompat.requestPermissions(this, new String[]{Manifest.permission.CAMERA}, CAMERA_PERMISSION_REQUEST);
    }

    private void showImageSourceDialog() {
        android.app.AlertDialog.Builder builder = new android.app.AlertDialog.Builder(this);
        builder.setTitle("Select Image Source");
        builder.setItems(new CharSequence[]{"Take Photo", "Choose from Gallery"}, (dialog, which) -> {
            if (which == 0) {
                // Take photo
                if (checkCameraPermission()) {
                    openCamera();
                } else {
                    requestCameraPermission();
                }
            } else {
                // Choose from gallery
                openGallery();
            }
        });
        builder.show();
    }

    private void openCamera() {
        Intent takePictureIntent = new Intent(MediaStore.ACTION_IMAGE_CAPTURE);
        if (takePictureIntent.resolveActivity(getPackageManager()) != null) {
            startActivityForResult(takePictureIntent, CAMERA_CAPTURE_REQUEST);
        } else {
            Toast.makeText(this, "Camera not available", Toast.LENGTH_SHORT).show();
        }
    }

    private void openGallery() {
        Intent pickPhoto = new Intent(Intent.ACTION_PICK, MediaStore.Images.Media.EXTERNAL_CONTENT_URI);
        pickPhoto.setType("image/*");
        if (pickPhoto.resolveActivity(getPackageManager()) != null) {
            startActivityForResult(pickPhoto, GALLERY_PICK_REQUEST);
        } else {
            Toast.makeText(this, "Gallery not available", Toast.LENGTH_SHORT).show();
        }
    }

    @Override
    public void onRequestPermissionsResult(int requestCode, @NonNull String[] permissions, @NonNull int[] grantResults) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults);
        if (requestCode == CAMERA_PERMISSION_REQUEST) {
            if (grantResults.length > 0 && grantResults[0] == PackageManager.PERMISSION_GRANTED) {
                openCamera();
            } else {
                Toast.makeText(this, "Camera permission is required to take invoice photo", Toast.LENGTH_LONG).show();
            }
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        
        if (resultCode == RESULT_OK) {
            if (requestCode == CAMERA_CAPTURE_REQUEST) {
                // Handle camera result
                if (data != null && data.getExtras() != null) {
                    Bitmap imageBitmap = (Bitmap) data.getExtras().get("data");
                    if (imageBitmap != null) {
                        photoPreview.setImageBitmap(imageBitmap);
                        photoPreview.setVisibility(View.VISIBLE);
                        cameraButton.setText("📷 Retake Photo");
                        updateSubmitButtonState();
                    }
                }
            } else if (requestCode == GALLERY_PICK_REQUEST) {
                // Handle gallery result
                if (data != null && data.getData() != null) {
                    Uri imageUri = data.getData();
                    try {
                        Bitmap imageBitmap = MediaStore.Images.Media.getBitmap(getContentResolver(), imageUri);
                        if (imageBitmap != null) {
                            photoPreview.setImageBitmap(imageBitmap);
                            photoPreview.setVisibility(View.VISIBLE);
                            cameraButton.setText("📷 Retake Photo");
                            updateSubmitButtonState();
                        }
                    } catch (IOException e) {
                        Log.e("SignActivity", "Error loading image from gallery", e);
                        Toast.makeText(this, "Error loading image", Toast.LENGTH_SHORT).show();
                    }
                }
            }
        }
    }

    private void selectSatisfaction(String satisfaction, Button button) {
        selectedSatisfaction = satisfaction;
        
        // Reset all buttons
        happyButton.setAlpha(1.0f);
        neutralButton.setAlpha(1.0f);
        sadButton.setAlpha(1.0f);
        
        // Highlight selected button
        button.setAlpha(0.7f);
        
        updateSubmitButtonState();
    }

    private void updateSubmitButtonState() {
        boolean hasPhoto = photoPreview.getVisibility() == View.VISIBLE && photoPreview.getDrawable() != null;
        boolean hasSignature = signatureView.hasSignature();
        boolean hasSatisfaction = selectedSatisfaction != null;

        submitButton.setEnabled(hasPhoto && hasSignature && hasSatisfaction);
        
        if (submitButton.isEnabled()) {
            submitButton.setAlpha(1.0f);
        } else {
            submitButton.setAlpha(0.5f);
        }
    }

    private void submitDelivery() {
        if (!signatureView.hasSignature()) {
            Toast.makeText(this, "Please provide a signature", Toast.LENGTH_SHORT).show();
            return;
        }

        if (selectedSatisfaction == null) {
            Toast.makeText(this, "Please select a satisfaction level", Toast.LENGTH_SHORT).show();
            return;
        }

        if (photoPreview.getVisibility() != View.VISIBLE || photoPreview.getDrawable() == null) {
            Toast.makeText(this, "Please take a photo of the invoice", Toast.LENGTH_SHORT).show();
            return;
        }

        // Disable submit button to prevent multiple submissions
        submitButton.setEnabled(false);
        
        // Show progress dialog
        showProgressDialog("Processing images...");

        // Process images in background thread to prevent UI freezing
        new Thread(() -> {
            try {
                // Get bitmaps on background thread
                Bitmap signatureBitmap = signatureView.getSignatureBitmap();
                Bitmap photoBitmap = null;
                if (photoPreview.getDrawable() != null) {
                    photoBitmap = ((android.graphics.drawable.BitmapDrawable) photoPreview.getDrawable()).getBitmap();
                }

                // Update progress
                runOnUiThread(() -> {
                    if (progressDialog != null) {
                        progressDialog.setMessage("Compressing images...");
                    }
                });

                // Compress and convert to base64
                String signatureBase64 = compressAndEncodeToBase64(signatureBitmap, 800, 80); // Max 800px, 80% quality
                String photoBase64 = compressAndEncodeToBase64(photoBitmap, 1920, 75); // Max 1920px, 75% quality

                // Get other data
                String comment = commentInput.getText().toString().trim();
                String weight = weightInput.getText().toString().trim();

                // Update progress
                runOnUiThread(() -> {
                    if (progressDialog != null) {
                        progressDialog.setMessage("Sending data...");
                    }
                });

                // Send to backend
                sendDeliveryDataToBackend(signatureBase64, photoBase64, comment, weight);

            } catch (Exception e) {
                Log.e("SignActivity", "Error processing delivery data", e);
                runOnUiThread(() -> {
                    dismissProgressDialog();
                    submitButton.setEnabled(true);
                    Toast.makeText(SignActivity.this, "Error processing data: " + e.getMessage(), Toast.LENGTH_LONG).show();
                });
            }
        }).start();
    }

    private String compressAndEncodeToBase64(Bitmap bitmap, int maxDimension, int quality) {
        if (bitmap == null) {
            return "";
        }

        try {
            // Calculate scaling to fit within max dimension while maintaining aspect ratio
            int width = bitmap.getWidth();
            int height = bitmap.getHeight();
            float scale = Math.min((float) maxDimension / width, (float) maxDimension / height);
            
            if (scale < 1.0f) {
                // Scale down if needed
                int newWidth = Math.round(width * scale);
                int newHeight = Math.round(height * scale);
                bitmap = Bitmap.createScaledBitmap(bitmap, newWidth, newHeight, true);
            }

            // Compress to JPEG
            ByteArrayOutputStream outputStream = new ByteArrayOutputStream();
            bitmap.compress(Bitmap.CompressFormat.JPEG, quality, outputStream);
            byte[] imageBytes = outputStream.toByteArray();

            // Convert to base64
            return android.util.Base64.encodeToString(imageBytes, android.util.Base64.NO_WRAP);
        } catch (Exception e) {
            Log.e("SignActivity", "Error compressing image", e);
            return "";
        }
    }

    private int getSatisfactionNumber(String satisfaction) {
        if (satisfaction == null) {
            return 0;
        }
        switch (satisfaction.toLowerCase()) {
            case "happy":
                return 1;
            case "neutral":
                return 2;
            case "sad":
                return 3;
            default:
                return 0;
        }
    }

    private void sendDeliveryDataToBackend(String signatureBase64, String photoBase64, String comment, String weight) {
        try {
            // Create JSON body
            JSONObject jsonBody = new JSONObject();
            jsonBody.put("delivery_id", deliveryId);
            jsonBody.put("signature", signatureBase64);
            jsonBody.put("invoice_photo", photoBase64);
            jsonBody.put("comment", comment);
            jsonBody.put("weight", weight);
            // Convert satisfaction string to number: happy=1, neutral=2, sad=3
            int satisfactionNumber = getSatisfactionNumber(selectedSatisfaction);
            jsonBody.put("satisfaction", satisfactionNumber);

            RequestBody body = RequestBody.create(
                    jsonBody.toString(),
                    MediaType.parse("application/json; charset=utf-8")
            );

            // Build request
            Request request = new Request.Builder()
                    .url(global.serverUrl + "/app/sign_delivery")
                    .post(body)
                    .addHeader("Content-Type", "application/json")
                    .build();

            // Execute request asynchronously
            httpClient.newCall(request).enqueue(new Callback() {
                @Override
                public void onFailure(@NonNull Call call, @NonNull IOException e) {
                    runOnUiThread(() -> {
                        dismissProgressDialog();
                        submitButton.setEnabled(true);
                        Toast.makeText(SignActivity.this, "Failed to submit delivery: " + e.getMessage(), Toast.LENGTH_LONG).show();
                        Log.e("SignActivity", "Failed to submit delivery", e);
                    });
                }

                @Override
                public void onResponse(@NonNull Call call, @NonNull Response response) throws IOException {
                    final String responseBody = response.body() != null ? response.body().string() : "";
                    runOnUiThread(() -> {
                        dismissProgressDialog();
                        if (response.isSuccessful()) {
                            // Get current location and send to backend
                            sendSignCoordinate();
                            
                            Toast.makeText(SignActivity.this, "Delivery validated successfully!", Toast.LENGTH_SHORT).show();
                            // Return to previous activity
                            setResult(RESULT_OK);
                            finish();
                        } else {
                            submitButton.setEnabled(true);
                            Toast.makeText(SignActivity.this, "Failed to submit delivery: " + responseBody, Toast.LENGTH_LONG).show();
                            Log.e("SignActivity", "Failed to submit delivery: " + responseBody);
                        }
                    });
                }
            });
        } catch (Exception e) {
            Log.e("SignActivity", "Error sending delivery data", e);
            runOnUiThread(() -> {
                dismissProgressDialog();
                submitButton.setEnabled(true);
                Toast.makeText(this, "Error sending data: " + e.getMessage(), Toast.LENGTH_LONG).show();
            });
        }
    }

    private void showProgressDialog(String message) {
        runOnUiThread(() -> {
            if (progressDialog == null) {
                progressDialog = new android.app.ProgressDialog(this);
                progressDialog.setCancelable(false);
            }
            progressDialog.setMessage(message);
            progressDialog.show();
        });
    }

    private void dismissProgressDialog() {
        runOnUiThread(() -> {
            if (progressDialog != null && progressDialog.isShowing()) {
                progressDialog.dismiss();
            }
        });
    }

    private void sendSignCoordinate() {

        Log.d("SignActivity", "Requesting GPS location for sign coordinate...");
        
        // Get current location using GPS
        Global.getCurrentLocation(this, new Global.LocationCallback() {
            @Override
            public void onLocationReceived(double latitude, double longitude, double altitude) {

                Log.d("SignActivity", "Sending to backend: /app/sign_coordinate");
                // Send location to backend
                sendCoordinateToBackend(deliveryId, latitude, longitude, altitude);
            }

            @Override
            public void onLocationError(String error) {
                Log.e("SignActivity", "Failed to get location: " + error);
                // Try to send with stored location as fallback
                android.content.SharedPreferences prefs = getSharedPreferences("GLNC_Prefs", Context.MODE_PRIVATE);
                double latitude = prefs.getFloat("latitude", 0.0f);
                double longitude = prefs.getFloat("longitude", 0.0f);
                double altitude = prefs.getFloat("altitude", 0.0f);
                
                // Only send if we have a valid stored location
                if (latitude != 0.0 || longitude != 0.0) {
                    Log.w("SignActivity", "Using stored location as fallback: " + latitude + ", " + longitude);
                    sendCoordinateToBackend(deliveryId, latitude, longitude, altitude);
                } else {
                    Log.e("SignActivity", "No location available (GPS failed and no stored location)");
                }
            }
        });
    }

    private void sendCoordinateToBackend(String deliveryId, double latitude, double longitude, double altitude) {
        new Thread(() -> {
            try {
                // Get user_id from SharedPreferences
                android.content.SharedPreferences prefs = getSharedPreferences("GLNC_Prefs", Context.MODE_PRIVATE);
                String userId = prefs.getString("user_id", "");
                
                if (userId.isEmpty()) {
                    Log.w("SignActivity", "User ID not found, skipping coordinate send");
                    return;
                }
                
                // Create JSON body
                JSONObject jsonBody = new JSONObject();
                jsonBody.put("delivery_id", deliveryId);
                jsonBody.put("user_id", userId);
                jsonBody.put("latitude", latitude);
                jsonBody.put("longitude", longitude);
                jsonBody.put("altitude", altitude);


                Log.d("SignActivity", "================================");

                RequestBody body = RequestBody.create(
                        jsonBody.toString(),
                        MediaType.parse("application/json; charset=utf-8")
                );

                // Build request
                Request request = new Request.Builder()
                        .url(global.serverUrl + "/app/sign_coordinate")
                        .post(body)
                        .addHeader("Content-Type", "application/json")
                        .build();

                // Execute request asynchronously
                httpClient.newCall(request).enqueue(new Callback() {
                    @Override
                    public void onFailure(@NonNull Call call, @NonNull IOException e) {
                        Log.e("SignActivity", "Failed to send coordinate: " + e.getMessage());
                        // Don't show error to user as sign was already successful
                    }

                    @Override
                    public void onResponse(@NonNull Call call, @NonNull Response response) throws IOException {
                        final String responseBody = response.body() != null ? response.body().string() : "";
                        if (response.isSuccessful()) {

                            Log.d("SignActivity", "=========================================");
                        } else {

                            Log.e("SignActivity", "===================================");
                        }
                    }
                });
            } catch (Exception e) {
                Log.e("SignActivity", "Error sending coordinate", e);
            }
        }).start();
    }

    // Custom Signature View
    public static class SignatureView extends View {
        private Path path;
        private Paint paint;
        private boolean hasSignature = false;
        private OnSignatureChangeListener listener;

        public interface OnSignatureChangeListener {
            void onSignatureChanged();
        }

        public SignatureView(android.content.Context context) {
            super(context);
            init();
        }

        public SignatureView(android.content.Context context, android.util.AttributeSet attrs) {
            super(context, attrs);
            init();
        }

        private void init() {
            path = new Path();
            paint = new Paint();
            paint.setAntiAlias(true);
            paint.setColor(Color.BLACK);
            paint.setStyle(Paint.Style.STROKE);
            paint.setStrokeJoin(Paint.Join.ROUND);
            paint.setStrokeCap(Paint.Cap.ROUND);
            paint.setStrokeWidth(4f);
            setBackgroundColor(Color.TRANSPARENT);
        }

        public void setOnSignatureChangeListener(OnSignatureChangeListener listener) {
            this.listener = listener;
        }

        @Override
        protected void onDraw(Canvas canvas) {
            super.onDraw(canvas);
            canvas.drawPath(path, paint);
        }

        @Override
        public boolean onTouchEvent(MotionEvent event) {
            float x = event.getX();
            float y = event.getY();

            switch (event.getAction()) {
                case MotionEvent.ACTION_DOWN:
                    // Request parent not to intercept touch events
                    getParent().requestDisallowInterceptTouchEvent(true);
                    path.moveTo(x, y);
                    return true;
                case MotionEvent.ACTION_MOVE:
                    // Continue to prevent parent from intercepting
                    getParent().requestDisallowInterceptTouchEvent(true);
                    path.lineTo(x, y);
                    hasSignature = true;
                    if (listener != null) {
                        listener.onSignatureChanged();
                    }
                    break;
                case MotionEvent.ACTION_UP:
                case MotionEvent.ACTION_CANCEL:
                    // Allow parent to intercept again when done
                    getParent().requestDisallowInterceptTouchEvent(false);
                    break;
                default:
                    return false;
            }
            invalidate();
            return true;
        }

        public void clear() {
            path.reset();
            hasSignature = false;
            if (listener != null) {
                listener.onSignatureChanged();
            }
            invalidate();
        }

        public boolean hasSignature() {
            return hasSignature;
        }

        public Bitmap getSignatureBitmap() {
            Bitmap bitmap = Bitmap.createBitmap(getWidth(), getHeight(), Bitmap.Config.ARGB_8888);
            Canvas canvas = new Canvas(bitmap);
            canvas.drawColor(Color.WHITE);
            draw(canvas);
            return bitmap;
        }
    }
}

