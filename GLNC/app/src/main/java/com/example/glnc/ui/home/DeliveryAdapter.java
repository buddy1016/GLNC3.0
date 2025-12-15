package com.example.glnc.ui.home;

import android.graphics.Paint;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.cardview.widget.CardView;
import androidx.core.content.ContextCompat;
import androidx.recyclerview.widget.RecyclerView;

import com.example.glnc.R;

import java.util.ArrayList;
import java.util.List;

public class DeliveryAdapter extends RecyclerView.Adapter<RecyclerView.ViewHolder> {
    private static final int TYPE_HEADER = 0;
    private static final int TYPE_ITEM = 1;

    private List<Object> items = new ArrayList<>(); // Can be Delivery or String (header)
    private OnItemClickListener itemClickListener;

    public interface OnItemClickListener {
        void onItemClick(Delivery delivery);
    }

    public void setOnItemClickListener(OnItemClickListener listener) {
        this.itemClickListener = listener;
    }

    public void setDeliveries(List<Delivery> deliveries) {
        items.clear();
        if (deliveries == null || deliveries.isEmpty()) {
            notifyDataSetChanged();
            return;
        }

        String currentSection = null;
        for (Delivery delivery : deliveries) {
            String section = null;
            if (delivery.isInProgress()) {
                section = "IN PROGRESS";
            } else if (delivery.isCompleted()) {
                section = "COMPLETED";
            } else if (delivery.isCancelled()) {
                section = "CANCELLED";
            }
            
            if (section != null && !section.equals(currentSection)) {
                items.add(section); // Add header
                currentSection = section;
            }
            items.add(delivery); // Add delivery item
        }
        
        notifyDataSetChanged();
    }

    @Override
    public int getItemViewType(int position) {
        Object item = items.get(position);
        return item instanceof String ? TYPE_HEADER : TYPE_ITEM;
    }

    @NonNull
    @Override
    public RecyclerView.ViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        if (viewType == TYPE_HEADER) {
            View view = LayoutInflater.from(parent.getContext())
                    .inflate(R.layout.item_section_header, parent, false);
            return new HeaderViewHolder(view);
        } else {
            View view = LayoutInflater.from(parent.getContext())
                    .inflate(R.layout.item_delivery, parent, false);
            return new DeliveryViewHolder(view);
        }
    }

    @Override
    public void onBindViewHolder(@NonNull RecyclerView.ViewHolder holder, int position) {
        Object item = items.get(position);

        if (holder instanceof HeaderViewHolder) {
            HeaderViewHolder headerHolder = (HeaderViewHolder) holder;
            String headerText = (String) item;
            headerHolder.headerText.setText(headerText);
        } else if (holder instanceof DeliveryViewHolder) {
            DeliveryViewHolder deliveryHolder = (DeliveryViewHolder) holder;
            deliveryHolder.bind((Delivery) item, itemClickListener);
        }
    }

    @Override
    public int getItemCount() {
        return items.size();
    }

    static class DeliveryViewHolder extends RecyclerView.ViewHolder {
        private View leftBorder;
        private TextView deliveryTime;
        private TextView deliveryClient;
        private TextView statusBadge;

        public DeliveryViewHolder(@NonNull View itemView) {
            super(itemView);
            leftBorder = itemView.findViewById(R.id.left_border);
            deliveryTime = itemView.findViewById(R.id.delivery_time);
            deliveryClient = itemView.findViewById(R.id.delivery_client);
            statusBadge = itemView.findViewById(R.id.status_badge);
        }

        public void bind(Delivery delivery, OnItemClickListener itemListener) {
            deliveryTime.setText(delivery.getTime());
            deliveryClient.setText(delivery.getClient());

            // Make entire card clickable
            itemView.setOnClickListener(v -> {
                if (itemListener != null) {
                    itemListener.onItemClick(delivery);
                }
            });

            // Reset styling for all items first
            itemView.setAlpha(1.0f);
            deliveryTime.setPaintFlags(deliveryTime.getPaintFlags() & (~Paint.STRIKE_THRU_TEXT_FLAG));
            deliveryClient.setPaintFlags(deliveryClient.getPaintFlags() & (~Paint.STRIKE_THRU_TEXT_FLAG));
            
            // Reset border width to default (4dp)
            ViewGroup.LayoutParams params = leftBorder.getLayoutParams();
            params.width = (int) (4 * itemView.getContext().getResources().getDisplayMetrics().density);
            leftBorder.setLayoutParams(params);
            
            if (delivery.isInProgress()) {
                // IN PROGRESS: Teal border, purple text, full opacity
                leftBorder.setBackgroundColor(ContextCompat.getColor(itemView.getContext(), R.color.teal_200));
                statusBadge.setText("IN PROGRESS");
                statusBadge.setBackgroundResource(R.drawable.status_badge_background);
                deliveryTime.setTextColor(ContextCompat.getColor(itemView.getContext(), R.color.purple_500));
                deliveryClient.setTextColor(ContextCompat.getColor(itemView.getContext(), android.R.color.black));
                itemView.setAlpha(1.0f);
            } else if (delivery.isCompleted()) {
                // COMPLETED: Gray border, gray text, reduced opacity
                leftBorder.setBackgroundColor(ContextCompat.getColor(itemView.getContext(), android.R.color.darker_gray));
                statusBadge.setText("COMPLETED");
                statusBadge.setBackgroundResource(R.drawable.status_badge_completed);
                deliveryTime.setTextColor(ContextCompat.getColor(itemView.getContext(), android.R.color.darker_gray));
                deliveryClient.setTextColor(ContextCompat.getColor(itemView.getContext(), android.R.color.darker_gray));
                itemView.setAlpha(0.7f);
            } else if (delivery.isCancelled()) {
                // CANCELLED: Red border (thicker), red text, strikethrough, distinct appearance
                params = leftBorder.getLayoutParams();
                params.width = (int) (8 * itemView.getContext().getResources().getDisplayMetrics().density); // 8dp instead of 4dp
                leftBorder.setLayoutParams(params);
                leftBorder.setBackgroundColor(ContextCompat.getColor(itemView.getContext(), android.R.color.holo_red_dark));
                statusBadge.setText("CANCELLED");
                statusBadge.setBackgroundResource(R.drawable.status_badge_cancelled);
                // Red text with strikethrough for cancelled
                int redColor = ContextCompat.getColor(itemView.getContext(), android.R.color.holo_red_dark);
                deliveryTime.setTextColor(redColor);
                deliveryClient.setTextColor(redColor);
                deliveryTime.setPaintFlags(deliveryTime.getPaintFlags() | Paint.STRIKE_THRU_TEXT_FLAG);
                deliveryClient.setPaintFlags(deliveryClient.getPaintFlags() | Paint.STRIKE_THRU_TEXT_FLAG);
                itemView.setAlpha(0.8f);
            } else {
                // Default case - should not happen, but handle gracefully
                leftBorder.setBackgroundColor(ContextCompat.getColor(itemView.getContext(), android.R.color.darker_gray));
                statusBadge.setText("UNKNOWN");
                statusBadge.setBackgroundResource(R.drawable.status_badge_completed);
                deliveryTime.setTextColor(ContextCompat.getColor(itemView.getContext(), android.R.color.darker_gray));
                deliveryClient.setTextColor(ContextCompat.getColor(itemView.getContext(), android.R.color.darker_gray));
            }
        }
    }

    static class HeaderViewHolder extends RecyclerView.ViewHolder {
        private TextView headerText;

        public HeaderViewHolder(@NonNull View itemView) {
            super(itemView);
            headerText = itemView.findViewById(R.id.section_header_text);
        }
    }
}
