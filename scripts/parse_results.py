import xml.etree.ElementTree as ET

tree = ET.parse('/tmp/results.xml')
root = tree.getroot()

male_tests = []
female_tests = []
for test in root.findall('.//test-case'):
    fullname = test.attrib.get('fullname', '')
    result = test.attrib.get('result', '')
    if 'MalePlayerVisualTests' in fullname:
        male_tests.append((fullname, result))
    elif 'FemalePlayerVisualTests' in fullname:
        female_tests.append((fullname, result))

print(f"MalePlayerVisualTests: {len(male_tests)} tests.")
male_passed = sum(1 for name, res in male_tests if res == 'Passed')
print(f"Passed: {male_passed}/{len(male_tests)}")
for name, res in male_tests:
    if res != 'Passed':
        print(f" - {name}: {res}")

print(f"\nFemalePlayerVisualTests: {len(female_tests)} tests.")
female_passed = sum(1 for name, res in female_tests if res == 'Passed')
print(f"Passed: {female_passed}/{len(female_tests)}")
for name, res in female_tests:
    if res != 'Passed':
        print(f" - {name}: {res}")
