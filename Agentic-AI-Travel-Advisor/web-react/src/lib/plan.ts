import type { CreateItineraryRequest, TravelPlan } from '../api/types';

export function planToItinerary(plan: TravelPlan, conversationId?: number): CreateItineraryRequest {
  const parts = [plan.hotels.find((h) => h.selected)?.name, plan.travelPackages.find((p) => p.selected)?.title].filter(Boolean);
  return {
    title: `${plan.destination} trip`,
    startDate: plan.startDate,
    endDate: plan.endDate,
    destinationId: plan.destinationId ?? null,
    estimatedCost: plan.estimatedTotal,
    budget: plan.budget,
    travelers: plan.travelers,
    conversationId: conversationId ?? null,
    summary: `${plan.travelers} traveler${plan.travelers === 1 ? '' : 's'}, ${plan.duration} days${parts.length ? ` · ${parts.join(' + ')}` : ''}`,
    items: plan.itinerary.flatMap((day) =>
      day.items.map((item, index) => ({
        dayNumber: day.day,
        title: item.title,
        description: item.description ?? null,
        startTime: /^\d{2}:\d{2}$/.test(item.time) ? `${item.time}:00` : null,
        sortOrder: index,
      })),
    ),
  };
}
