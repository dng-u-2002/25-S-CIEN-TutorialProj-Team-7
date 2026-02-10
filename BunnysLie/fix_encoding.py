import os
import glob

# 변환할 대상 확장자 (필요하면 .txt, .json 등 추가)
TARGET_EXTENSIONS = ['*.cs']

# 인코딩 변환 함수
def convert_encoding(file_path):
    # 1. 먼저 UTF-8로 읽어봅니다. (이미 정상이면 패스)
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            f.read()
        return False # 변환 필요 없음
    except UnicodeDecodeError:
        pass # UTF-8이 아니므로 변환 시도

    # 2. CP949(EUC-KR 확장)로 읽어서 UTF-8로 다시 저장합니다.
    try:
        with open(file_path, 'r', encoding='cp949') as f:
            content = f.read()
        
        with open(file_path, 'w', encoding='utf-8-sig') as f: # BOM 포함 저장
            f.write(content)
        
        print(f"✅ 변환 완료: {file_path}")
        return True
    except Exception as e:
        print(f"❌ 변환 실패 (알 수 없는 인코딩): {file_path} / 에러: {e}")
        return False

def main():
    print("--- 인코딩 일괄 변환 시작 ---")
    converted_count = 0
    
    # Assets 폴더 하위의 모든 파일을 뒤집니다.
    for ext in TARGET_EXTENSIONS:
        # recursive=True로 하위 폴더까지 검색
        files = glob.glob(f'./Assets/**/*.{ext.replace("*.", "")}', recursive=True)
        
        for file_path in files:
            if convert_encoding(file_path):
                converted_count += 1

    print(f"--- 작업 완료: 총 {converted_count}개 파일 변환됨 ---")

if __name__ == "__main__":
    main()