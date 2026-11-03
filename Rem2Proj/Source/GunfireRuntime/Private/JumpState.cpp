#include "JumpState.h"
#include "CliffConditionParams_Default.h"
#include "ERootMotionVelocityMethod.h"

UJumpState::UJumpState() {
    this->RootMotionMethod = ERootMotionVelocityMethod::ApplyGravityWithDownwardRootMotion;
    this->bDisableCollisionOnEntry = false;
    this->LerpEasesIn = false;
    this->Type = EJumpStateType::Impulse;
    this->CliffParams = UCliffConditionParams_Default::StaticClass();
    this->JumpDistance = 0.00f;
    this->LedgeEvent = TEXT("ReadyToJump");
    this->LandingParams = NULL;
    this->bDisableGravityUntilJumpImpulse = true;
}


