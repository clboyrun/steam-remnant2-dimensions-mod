#include "RopeActor.h"
#include "Components/SphereComponent.h"
#include "PhysicsEngine/PhysicsConstraintComponent.h"

ARopeActor::ARopeActor(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->EndpointRopeAttachment = CreateDefaultSubobject<USphereComponent>(TEXT("EndpointRopeAttachment"));
    this->SourceRopeAttachment = CreateDefaultSubobject<USphereComponent>(TEXT("SourceRopeAttachment"));
    this->EndpointRopeAttachment->SetupAttachment(EndpointConstraint);
    this->SourceRopeAttachment->SetupAttachment(SourceConstraint);
}


