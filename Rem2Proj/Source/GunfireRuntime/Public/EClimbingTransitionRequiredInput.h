#pragma once
#include "CoreMinimal.h"
#include "EClimbingTransitionRequiredInput.generated.h"

UENUM()
enum class EClimbingTransitionRequiredInput : int32 {
    Any,
    None,
    MoveInputOnly,
    PromptOnly,
    MoveInputAndPrompt,
    RequiresMoveInput,
    RequiresPrompt,
};

