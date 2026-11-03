#include "Portal.h"
#include "Components/BoxComponent.h"
#include "Components/SceneCaptureComponent2D.h"
#include "Components/SceneComponent.h"
#include "Net/UnrealNetwork.h"

APortal::APortal(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bReplicates = true;
    const FProperty* p_RemoteRole = GetClass()->FindPropertyByName("RemoteRole");
    (*p_RemoteRole->ContainerPtrToValuePtr<TEnumAsByte<ENetRole>>(this)) = ROLE_SimulatedProxy;
    this->RootComponent = CreateDefaultSubobject<USceneComponent>(TEXT("Root"));
    this->SceneCapture = CreateDefaultSubobject<USceneCaptureComponent2D>(TEXT("SceneCapture"));
    this->PortalRoot = (USceneComponent*)RootComponent;
    this->PortalTriggerBox = CreateDefaultSubobject<UBoxComponent>(TEXT("PortalTriggerBox"));
    this->UprightCosineThreshold = 0.50f;
    this->DefaultEnterTime = 0.40f;
    this->DefaultExitTime = 0.40f;
    this->CoplanarAlignmentAlpha = 0.80f;
    this->EnterScaleStartAlpha = 0.20f;
    this->EnterScaleStopAlpha = 0.80f;
    this->ExitScaleStartAlpha = 0.20f;
    this->ExitScaleStopAlpha = 0.80f;
    this->bAllowAICharacterTraversal = true;
    this->CharacterCooldown = 1.00f;
    this->MaximumEntryDistance = 0.00f;
    this->EnterRadius = 0.00f;
    this->EnterCosineThresh = -0.80f;
    this->bSquareRenderTarget = false;
    this->MaximumRenderDistance = 0.00f;
    this->DrawRadius = 0.00f;
    this->CharacterSnapToGroundDistance = 0.00f;
    this->CharacterMinimumEnterHeightScale = 0.00f;
    this->bValidateExitCollision = true;
    this->DefaultEnterVFX = NULL;
    this->DefaultExitVFX = NULL;
    this->bIgnoreHitscanImpactEffects = true;
    this->DefaultHitscanEnterVFX = NULL;
    this->DefaultHitscanExitVFX = NULL;
    this->bPoolRenderTarget = false;
    this->TargetPortal = NULL;
    this->RenderTarget = NULL;
    this->AttachComponent = NULL;
    this->SceneCapture->SetupAttachment(RootComponent);
    this->PortalTriggerBox->SetupAttachment(RootComponent);
}


FRotator APortal::TransformRotationBetweenPortals(FRotator Rotation, APortal* EntryPortal, APortal* ExitPortal) {
    return FRotator{};
}

FQuat APortal::TransformQuatBetweenPortals(FQuat Rotation, APortal* EntryPortal, APortal* ExitPortal) {
    return FQuat{};
}

FVector APortal::TransformLocationBetweenPortals(FVector Location, APortal* EntryPortal, APortal* ExitPortal) {
    return FVector{};
}

FVector APortal::TransformDirectionBetweenPortals(FVector Direction, APortal* EntryPortal, APortal* ExitPortal) {
    return FVector{};
}

bool APortal::ShouldRenderPortal_Implementation() const {
    return false;
}


void APortal::SetupFixedView_Implementation(USceneCaptureComponent2D* OtherSceneCapture) {
}

void APortal::SetTargetPortal(APortal* NewPortal) {
}

bool APortal::ProjectWorldLocationThroughPortal(APlayerController* Controller, FVector WorldLocation, APortal* EntryPortal, APortal* ExitPortal, FVector& ProjectedLocation) {
    return false;
}


void APortal::OnRep_TargetPortal() {
}

void APortal::MulticastStartTransition_Implementation(AActor* Actor, FVector_NetQuantize RelativeTargetLocation, FVector_NetQuantize RelativeTransitionAxis, APortal* TransitionTargetPortal, const TArray<AActor*>& ActorsToAttach, float EnterTime, float ExitTime) {
}

bool APortal::IsPortalUpright() const {
    return false;
}

bool APortal::IsActorEnteringPortal(AActor* Actor) const {
    return false;
}

bool APortal::IsActorDoingAnyPortalTransition(AActor* Actor) {
    return false;
}

bool APortal::HasAnyActorTransitions() const {
    return false;
}

APortal* APortal::GetTargetPortal() const {
    return NULL;
}

UTextureRenderTarget2D* APortal::GetRenderTarget() {
    return NULL;
}

EPortalRenderState APortal::GetPortalRenderState() const {
    return EPortalRenderState::Disabled;
}

FTransform APortal::GetEventHorizonTransform() const {
    return FTransform{};
}

FPlane APortal::GetEventHorizonPlane() const {
    return FPlane{};
}


bool APortal::EnterPortal(AActor* Actor, const FVector& WorldLocation) {
    return false;
}

FVector APortal::CalculateRelativeTransitionAxis(AActor* Actor, APortal* Portal) {
    return FVector{};
}

FVector APortal::CalculatePortalEntryLocationForPrediction(AActor* Projectile, const FVector& Velocity, APortal* Portal, const FVector& WorldLocation) {
    return FVector{};
}

FVector APortal::CalculatePortalEntryLocationForActor(AActor* Actor, APortal* Portal, const FVector& WorldLocation) {
    return FVector{};
}

FBox APortal::CalculateBoundsForActor(AActor* Actor, const FTransform Transform) {
    return FBox{};
}

bool APortal::ArePortalTransitionsEnabled_Implementation() const {
    return false;
}

bool APortal::ApplyEjectionForce_Implementation(AActor* Actor, APortal* EntryPortal, FVector& EjectionForce) {
    return false;
}

void APortal::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const {
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    
    DOREPLIFETIME(APortal, TargetPortal);
    DOREPLIFETIME(APortal, AttachComponent);
    DOREPLIFETIME(APortal, RelativeAttachTransform);
}


