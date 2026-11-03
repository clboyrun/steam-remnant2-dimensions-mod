#include "ReplicatedMovementComponent.h"
#include "Net/UnrealNetwork.h"

UReplicatedMovementComponent::UReplicatedMovementComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->BufferTime = 100;
    this->DistanceTolerance = 0.00f;
    this->RotationTolerance = 0.00f;
    this->bUseTeleportPhysics = false;
    this->RotationReplication = EReplicatedMovementRotationReplication::Walking;
    this->DefaultSmoothTime = 0.00f;
    this->DefaultVelocitySmoothTime = 0.00f;
    this->SpectatedVelocitySmoothTime = 0.05f;
    this->SimulatedToleranceSmoothTime = 0.10f;
    this->DetachSmoothTime = 0.10f;
    this->SpectatedSmoothTime = 0.00f;
    this->ExtrapolationSmoothTime = 0.04f;
    this->ExtrapolationTeleportThresh = 500.00f;
    this->RotationSmoothingRateScale = 0.25f;
    this->ExtrapolateFromSinglePointWithVelocity = false;
    this->BaseMovementOverride = NULL;
}

void UReplicatedMovementComponent::SetBaseMovementOverride(UPrimitiveComponent* Comp) {
}

void UReplicatedMovementComponent::ServerMoveUpdate_Implementation(const FReplicatedMovementInfo& Info) {
}
bool UReplicatedMovementComponent::ServerMoveUpdate_Validate(const FReplicatedMovementInfo& Info) {
    return true;
}

void UReplicatedMovementComponent::Reset() {
}

void UReplicatedMovementComponent::OnRep_MovementInfo() {
}

UPrimitiveComponent* UReplicatedMovementComponent::GetBaseMovementOverride() {
    return NULL;
}

void UReplicatedMovementComponent::ClientAckMove_Implementation(uint16 MoveTime) {
}

void UReplicatedMovementComponent::ClearBaseMovementOverride() {
}

void UReplicatedMovementComponent::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const {
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    
    DOREPLIFETIME(UReplicatedMovementComponent, InitialVelocityForExtrapolation);
    DOREPLIFETIME(UReplicatedMovementComponent, MovementInfo);
}


