#include "VitalityComponent.h"
#include "Net/UnrealNetwork.h"

UVitalityComponent::UVitalityComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->DefaultRegenDelay = 0.00f;
    this->InitialValue = EVitalityInitialValue::Max;
    this->Value = -1.00f;
}

void UVitalityComponent::Reset(bool Notify) {
}

void UVitalityComponent::OnRep_Value() {
}

bool UVitalityComponent::HasValidStatsComp() const {
    return false;
}

UVitalityComponent* UVitalityComponent::GetVitality(AActor* Actor, FName Vitality) {
    return NULL;
}

float UVitalityComponent::GetValuePercentage() const {
    return 0.0f;
}

float UVitalityComponent::GetValueMax() const {
    return 0.0f;
}

float UVitalityComponent::GetValue() const {
    return 0.0f;
}

FString UVitalityComponent::GetDebugInfo(int32 DebugLevel) {
    return TEXT("");
}

void UVitalityComponent::ClearRegenDelay() {
}

void UVitalityComponent::ApplyDeltaReplicated_Implementation(float Delta) {
}

void UVitalityComponent::ApplyDelta(float Delta) {
}

void UVitalityComponent::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const {
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    
    DOREPLIFETIME(UVitalityComponent, Value);
}


