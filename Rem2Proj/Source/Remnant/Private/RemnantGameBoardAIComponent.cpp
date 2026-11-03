#include "RemnantGameBoardAIComponent.h"

URemnantGameBoardAIComponent::URemnantGameBoardAIComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RandomMovePercent = 0;
    this->bScaleWithDifficulty = false;
    this->DifficultyModifier = -1;
}


